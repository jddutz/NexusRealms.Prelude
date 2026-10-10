using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude;

/// <summary>
/// Represents a combat character and owns the character's world-space visual.
/// </summary>
public sealed class Character : GameObject2D
{
    private readonly SpriteInstance _sprite;
    private SpriteInstance? _focusIndicator;
    private Vector2D<float> _indicatorWorldPerPixel = new(1f, 1f);
    private ITexture? _validIndicator;
    private ITexture? _invalidIndicator;
    private byte[]? _hitPixels;
    public Combat.DamageReceiver DamageReceiver { get; }
    private int _focus;
    public event Action<Character>? ResourcesChanged;
    public int Health
    {
        get => DamageReceiver.Health;
        set => DamageReceiver.Health = value;
    }
    public int Focus
    {
        get => _focus;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_focus == value) return;
            _focus = value;
            ResourcesChanged?.Invoke(this);
        }
    }
    public bool IsFocused { get; private set; }
    public bool IsValidTarget { get; private set; }
    public event Action<Character>? FocusChanged;
    public SpriteRenderer FocusRenderer { get; } = new() { IsVisible = false };
    private SpriteAnimationPlayer? _animationPlayer;
    private AsepriteSpriteSheet? _spriteSheet;
    private bool _staggering;
    public float VisualWidth => _sprite.Size.X;
    public float VisualHeight => _sprite.Size.Y;
    private float _elevation;
    private readonly AnimationPlayer<Color> _damagePlayer;
    private readonly AnimationPlayer<Combat.AttackImpact> _impactPlayer;
    private Color _normalColor;
    private bool _defeatPending;
    private readonly AnimationPlayer<Color> _defeatPlayer;
    private static readonly TimeSpan DamageFlashDuration = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan ImpactDuration = TimeSpan.FromMilliseconds(250);
    public bool IsShowingDamage => !_damagePlayer.IsCompleted;
    public bool IsShowingFeedback => IsShowingDamage || !_impactPlayer.IsCompleted
        || _staggering || _defeatPending || !_defeatPlayer.IsCompleted;
    private readonly SpriteRenderer _impactRenderer = new() { IsVisible = false };
    private readonly SpriteRenderer _impactIconRenderer = new() { IsVisible = false };
    private readonly SpriteInstance _impactSprite = new() { Anchor = new(0.5f, 0.5f) };
    private readonly SpriteInstance _impactIcon = new() { Anchor = new(0.5f, 0.5f) };
    private ITextureRegistry? _feedbackTextures;

    public void ConfigureAttackFeedback(ITextureRegistry textures, ulong layerMask)
    {
        _feedbackTextures = textures;
        _impactRenderer.RenderLayerMask = layerMask;
        _impactIconRenderer.RenderLayerMask = layerMask;
    }

    private void ShowImpact(Combat.AttackImpact impact)
    {
        if (_feedbackTextures is null) return;
        _impactPlayer.Enqueue(new Animation<Combat.AttackImpact>([new(impact, ImpactDuration)]));
    }

    private void ApplyImpact(Combat.AttackImpact impact)
    {
        // Draw above all formation rows, with ammunition above the effect.
        _impactRenderer.DrawOrder = 90;
        _impactIconRenderer.DrawOrder = 91;
        SetImpactTexture(_impactRenderer, _impactSprite, impact.EffectTexture, 0.8f);
        SetImpactTexture(_impactIconRenderer, _impactIcon, impact.IconTexture, 0.5f);
    }

    private void SetImpactTexture(SpriteRenderer renderer, SpriteInstance sprite, string? textureId, float size)
    {
        renderer.IsVisible = false;
        if (textureId is null) return;
        var texture = _feedbackTextures!.GetOrCreate(new ContentId(textureId));
        renderer.Texture = texture;
        var scale = VisualWidth * size / Math.Max(texture.Width, texture.Height);
        sprite.Size = new(texture.Width * scale, texture.Height * scale);
        sprite.Transform = Matrix4X4.CreateTranslation(0f, -VisualHeight * 0.55f - Elevation, 0f);
        renderer.IsVisible = true;
    }

    private void BeginDefeatAfterFeedback()
    {
        if (!_defeatPending || IsShowingDamage || !_impactPlayer.IsCompleted || _staggering) return;
        _defeatPending = false;
        _animationPlayer = null;
        // Keep the character instance and state; fade its artwork before hiding it.
        _defeatPlayer.Play(new Animation<Color>(Enumerable.Range(0, 10).Select(index =>
            new AnimationFrame<Color>(_normalColor.WithTransparency(_normalColor.A * (1f - index / 10f)),
                TimeSpan.FromMilliseconds(25)))));
    }

    /// <summary>Queues one tint for each atomic point of inflicted damage.</summary>
    private void ShowDamage(Combat.DamageTaken result)
    {
        var damage = result.Damage;
        if (_spriteSheet is not null)
        {
            PlayAnimation(_spriteSheet.GetAnimation("Stagger"));
            _staggering = true;
        }
        if (_damagePlayer.IsCompleted) _normalColor = _sprite.Color;
        var frames = new List<AnimationFrame<Color>>();
        foreach (var (type, amount) in damage.Typed)
        {
            var color = type switch
            {
                DamageType.Fire => new Color(1f, 0.45f, 0.1f, 1f),
                DamageType.Ice => new Color(0.25f, 0.65f, 1f, 1f),
                DamageType.Lightning => new Color(1f, 0.9f, 0.15f, 1f),
                DamageType.Poison => new Color(0.25f, 1f, 0.25f, 1f),
                _ => new Color(1f, 0.2f, 0.2f, 1f),
            };
            for (var point = 0; point < amount; point++) frames.Add(new(color, DamageFlashDuration));
        }
        _defeatPending |= result.IsLethal;
        _damagePlayer.Enqueue(new Animation<Color>(frames));
    }

    /// <summary>
    /// Initializes a character from its story definition and artwork texture.
    /// </summary>
    /// <param name="definition">The immutable story definition for the character.</param>
    /// <param name="texture">The texture used to render the character.</param>
    /// <param name="shadowTexture">The ground shadow rendered at the feet.</param>
    public Character(DataModel.CharacterData definition, ITexture texture, ITexture shadowTexture)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(shadowTexture);

        Definition = definition;
        DamageReceiver = new(definition.Health);
        DamageReceiver.HealthChanged += () => ResourcesChanged?.Invoke(this);
        DamageReceiver.DamageTaken += ShowDamage;
        DamageReceiver.ImpactOccurred += ShowImpact;
        Focus = definition.Focus;
        Texture = texture;
        ShadowRenderer = new SpriteRenderer { Texture = shadowTexture };
        ShadowRenderer.Add(
            new SpriteInstance
            {
                // Flatten the square source into a rectangular patch on the ground.
                Size = new(texture.Width * 1.10f, texture.Height * 0.12f),
                // Shift the ground patch up by one quarter of its height.
                Anchor = new(0.5f, 0.75f),
            }
        );
        AddComponent(ShadowRenderer);
        Renderer = new SpriteRenderer { Texture = texture };
        _sprite = new SpriteInstance
        {
            Size = new(texture.Width, texture.Height),
            Anchor = new(0.5f, 1f),
        };
        _defeatPlayer = new(color =>
        {
            _sprite.Color = color;
            foreach (var shadow in ShadowRenderer.Instances.Values)
                shadow.Color = Colors.White.WithTransparency(color.A);
        });
        _defeatPlayer.Completed += () =>
        {
            Renderer.IsVisible = false;
            ShadowRenderer.IsVisible = false;
            FocusRenderer.IsVisible = false;
            _impactRenderer.IsVisible = false;
            _impactIconRenderer.IsVisible = false;
        };
        _damagePlayer = new(color => _sprite.Color = color);
        _damagePlayer.Completed += () =>
        {
            _sprite.Color = _normalColor;
            BeginDefeatAfterFeedback();
        };
        _impactPlayer = new(ApplyImpact);
        _impactPlayer.Completed += () =>
        {
            _impactRenderer.IsVisible = false;
            _impactIconRenderer.IsVisible = false;
            BeginDefeatAfterFeedback();
        };
        SpriteId = Renderer.Add(_sprite);
        AddComponent(Renderer);
        AddComponent(FocusRenderer);
        _impactRenderer.Add(_impactSprite);
        _impactIconRenderer.Add(_impactIcon);
        AddComponent(_impactRenderer);
        AddComponent(_impactIconRenderer);
    }

    public void ConfigureAnimations(AsepriteSpriteSheet? sheet)
    {
        if (sheet is null) return;
        _spriteSheet = sheet;
        _sprite.Size = new(sheet.FrameWidth, sheet.FrameHeight);
        foreach (var shadow in ShadowRenderer.Instances.Values)
            shadow.Size = new(VisualWidth * 1.10f, VisualHeight * 0.12f);
        ShowIdle();
    }

    private void ShowIdle()
    {
        _animationPlayer = null;
        _sprite.TexCoord = _spriteSheet!.GetAnimation("Idle").Frames[0].TexCoord;
    }

    public void ConfigureFocusIndicator(ITexture valid, ITexture invalid, ulong layerMask)
    {
        _validIndicator = valid;
        _invalidIndicator = invalid;
        FocusRenderer.RenderLayerMask = layerMask;
        _focusIndicator = new SpriteInstance { Anchor = new(0.5f, 1f) };
        FocusRenderer.Add(_focusIndicator);
        RefreshFocusIndicator();
    }

    /// <summary>Compensates for formation scale and viewport zoom to keep symbols screen-sized.</summary>
    public void SetIndicatorScreenScale(float worldPerPixelX, float worldPerPixelY)
    {
        if (!float.IsFinite(worldPerPixelX) || !float.IsFinite(worldPerPixelY)
            || worldPerPixelX <= 0f || worldPerPixelY <= 0f)
            throw new ArgumentOutOfRangeException(nameof(worldPerPixelX));
        _indicatorWorldPerPixel = new(worldPerPixelX, worldPerPixelY);
        RefreshFocusIndicator();
    }
    internal void SetFocus(bool focused, bool validTarget)
    {
        if (IsFocused == focused && IsValidTarget == validTarget) return;
        var focusChanged = IsFocused != focused;
        IsFocused = focused;
        IsValidTarget = validTarget;
        RefreshFocusIndicator();
        if (focusChanged) FocusChanged?.Invoke(this);
    }

    private void RefreshFocusIndicator()
    {
        var texture = IsValidTarget ? _validIndicator : _invalidIndicator;
        if (_focusIndicator is null || texture is null)
        {
            FocusRenderer.IsVisible = false;
            return;
        }
        FocusRenderer.Texture = texture;
        const float screenSize = 48f;
        var fit = screenSize / Math.Max(texture.Width, texture.Height);
        _focusIndicator.Size = new(texture.Width * fit * _indicatorWorldPerPixel.X / Scale.X,
            texture.Height * fit * _indicatorWorldPerPixel.Y / Scale.Y);
        _focusIndicator.Transform = Matrix4X4.CreateTranslation(0f,
            -VisualHeight - Elevation - 8f * _indicatorWorldPerPixel.Y / Scale.Y, 0f);
        FocusRenderer.DrawOrder = 100;
        // Publish the drawable only after its selected texture and geometry are ready.
        FocusRenderer.IsVisible = IsFocused && Renderer.IsVisible;
    }

    /// <summary>Tests the rendered sprite's opaque pixels rather than its transparent rectangle.</summary>
    public bool HitTest(Vector2D<float> worldPosition)
    {
        if (!Renderer.IsVisible) return false;
        var x = (worldPosition.X - Position.X) / Scale.X + VisualWidth * 0.5f;
        var y = (worldPosition.Y - Position.Y) / Scale.Y + VisualHeight + Elevation;
        if (x < 0f || y < 0f || x >= VisualWidth || y >= VisualHeight) return false;
        if (_hitPixels is null)
        {
            _hitPixels = new byte[checked((int)Texture.Count * 4)];
            Texture.WriteTo(0, Texture.Count, ColorFormatEnum.RGBA8UNorm, _hitPixels);
        }
        var atlasX = Math.Clamp((int)(_sprite.TexCoord.X * Texture.Width + x), 0, (int)Texture.Width - 1);
        var atlasY = Math.Clamp((int)(_sprite.TexCoord.Y * Texture.Height + y), 0, (int)Texture.Height - 1);
        return _hitPixels[(atlasY * (int)Texture.Width + atlasX) * 4 + 3] > 16;
    }

    /// <summary>Gets the immutable story definition represented by this entity.</summary>
    public DataModel.CharacterData Definition { get; }

    /// <summary>Gets the texture used by the character's sprite.</summary>
    public ITexture Texture { get; }

    /// <summary>Gets the renderer owned by this character.</summary>
    public SpriteRenderer Renderer { get; }

    /// <summary>Gets the ground shadow renderer anchored at the character position.</summary>
    public SpriteRenderer ShadowRenderer { get; }

    /// <summary>Gets the stable identifier of the character's primary sprite instance.</summary>
    public SpriteInstanceId SpriteId { get; }

    /// <summary>
    /// Gets or sets the distance by which the visual is raised above the ground position.
    /// </summary>
    public float Elevation
    {
        get => _elevation;
        set
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));

            _elevation = value;
            _sprite.Transform = Matrix4X4.CreateTranslation(0f, -value, 0f);
            RefreshFocusIndicator();
        }
    }

    /// <summary>Assigns an animation to the primary sprite and starts it at its first frame.</summary>
    /// <param name="animation">The animation to play.</param>
    public void PlayAnimation(SpriteAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(animation);
        _animationPlayer = new SpriteAnimationPlayer(_sprite, animation);
    }

    /// <inheritdoc />
    public override void Update(double deltaTime)
    {
        if (!double.IsFinite(deltaTime) || deltaTime < 0d)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));

        var elapsed = TimeSpan.FromSeconds(deltaTime);
        _defeatPlayer.Advance(elapsed);
        _damagePlayer.Advance(elapsed);
        _impactPlayer.Advance(elapsed);
        if (!_impactPlayer.IsCompleted)
        {
            var transform = Matrix4X4.CreateTranslation(0f, -VisualHeight * 0.55f - Elevation, 0f);
            _impactSprite.Transform = transform;
            _impactIcon.Transform = transform;
        }
        BeginDefeatAfterFeedback();
        _animationPlayer?.Advance(elapsed);
        if (_staggering && _animationPlayer is { IsCompleted: true })
        {
            _staggering = false;
            ShowIdle();
            BeginDefeatAfterFeedback();
        }
        base.Update(deltaTime);
    }
}






