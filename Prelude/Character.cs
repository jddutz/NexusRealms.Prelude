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
    /// <summary>Shared mutable inventory, equipment, capabilities, and Focus.</summary>
    public Combat.CombatLoadout Loadout { get; }
    public event Action<Character>? ResourcesChanged;
    public int Health
    {
        get => DamageReceiver.Health;
        set => DamageReceiver.Health = value;
    }
    public int Focus
    {
        get => Loadout.Focus;
        set => Loadout.Focus = value;
    }
    public bool IsFocused { get; private set; }
    public bool IsValidTarget { get; private set; }
    public event Action<Character>? FocusChanged;
    public SpriteRenderer FocusRenderer { get; } = new() { IsVisible = false };
    private SpriteAnimationPlayer? _animationPlayer;
    private AsepriteSpriteSheet? _spriteSheet;
    private bool _staggering;
    public bool StaggerAnimationEnabled { get; set; } = true;
    public double AnimationSpeed { get; set; } = 1d;
    private bool _attacking;
    public double PlayAttack(string name)
    {
        if (_spriteSheet is null) return 0.5d;
        SpriteAnimation animation;
        try { animation = _spriteSheet.GetAnimation(name); }
        catch (KeyNotFoundException exception)
        {
            throw new InvalidOperationException($"Character {Definition.Id} has no animation named {name}.", exception);
        }
        PlayAnimation(animation);
        _attacking = true;
        return animation.Duration.TotalSeconds;
    }
    public static Color DamageColor(DamageType type) => type switch
    {
        DamageType.Fire => new Color(1f, 0.45f, 0.1f, 1f),
        DamageType.Ice => new Color(0.25f, 0.65f, 1f, 1f),
        DamageType.Lightning => new Color(1f, 0.9f, 0.15f, 1f),
        DamageType.Poison => new Color(0.25f, 1f, 0.25f, 1f),
        _ => new Color(1f, 0.2f, 0.2f, 1f),
    };
    public float VisualWidth => _sprite.Size.X;
    public float VisualHeight => _sprite.Size.Y;
    private float _elevation;
    private readonly AnimationPlayer<Color> _damagePlayer;
    private readonly AnimationPlayer<EffectPhase> _effectPlayer;
    private Color _normalColor;
    private bool _defeatPending;
    private readonly AnimationPlayer<Color> _defeatPlayer;
    private static readonly TimeSpan DamageFlashDuration = TimeSpan.FromMilliseconds(80);
    // Duration is elapsed time; stagger overlaps the final nominal 60 Hz frame.
    private static readonly TimeSpan DisplayFrame = TimeSpan.FromSeconds(1d / 60d);
    private static readonly TimeSpan EffectDuration = TimeSpan.FromMilliseconds(500);
    private sealed record EffectPhase(Combat.AttackImpact Impact, bool FinalFrame);
    private TimeSpan _effectPhaseRemaining;
    public bool IsShowingDamage => !_damagePlayer.IsCompleted;
    public bool IsShowingFeedback => IsShowingDamage || !_effectPlayer.IsCompleted
        || _staggering || _defeatPending || !_defeatPlayer.IsCompleted;
    private readonly SpriteRenderer _effectRenderer = new() { IsVisible = false };
    private readonly SpriteRenderer _effectIconRenderer = new() { IsVisible = false };
    private readonly SpriteInstance _effectSprite = new() { Anchor = new(0.5f, 0.5f) };
    private readonly SpriteInstance _effectIcon = new() { Anchor = new(0.5f, 0.5f) };
    private ITextureRegistry? _feedbackTextures;

    /// <summary>Configures textures and rendering for effect sprite overlays on the character.</summary>
    public void ConfigureEffectOverlays(ITextureRegistry textures, ulong layerMask)
    {
        _feedbackTextures = textures;
        _effectRenderer.RenderLayerMask = layerMask;
        _effectIconRenderer.RenderLayerMask = layerMask;
    }

    private void ShowAttackEffect(Combat.AttackImpact impact)
    {
        if (_feedbackTextures is null) return;
        _effectPlayer.Enqueue(new Animation<EffectPhase>([
            new(new(impact, false), EffectDuration - DisplayFrame),
            new(new(impact, true), DisplayFrame),
        ]));
    }

    private void ApplyEffect(EffectPhase phase)
    {
        _effectPhaseRemaining = phase.FinalFrame ? DisplayFrame : EffectDuration - DisplayFrame;
        if (phase.FinalFrame)
        {
            if (phase.Impact != Combat.AttackImpact.Blocked) StartStagger();
            return;
        }
        var impact = phase.Impact;
        // Draw above all formation rows, with ammunition above the effect.
        _effectRenderer.DrawOrder = 90;
        _effectIconRenderer.DrawOrder = 91;
        SetEffectTexture(_effectRenderer, _effectSprite, impact.EffectTexture, 0.5f);
        SetEffectTexture(_effectIconRenderer, _effectIcon, impact.IconTexture, 0.5f);
    }

    private void SetEffectTexture(SpriteRenderer renderer, SpriteInstance sprite, string? textureId, float size)
    {
        renderer.IsVisible = false;
        if (textureId is null) return;
        var texture = _feedbackTextures!.GetOrCreate(new ContentId(textureId));
        renderer.Texture = texture;
        var scale = Math.Max(VisualWidth, VisualHeight) * size / Math.Max(texture.Width, texture.Height);
        sprite.Size = new(texture.Width * scale, texture.Height * scale);
        sprite.Transform = Matrix4X4.CreateTranslation(0f, -VisualHeight * 0.55f - Elevation, 0f);
        renderer.IsVisible = true;
    }

    private void BeginDefeatAfterFeedback()
    {
        if (!_defeatPending || IsShowingDamage || !_effectPlayer.IsCompleted || _staggering) return;
        _defeatPending = false;
        _animationPlayer = null;
        // Keep the character instance and state; fade its artwork before hiding it.
        _defeatPlayer.Play(new Animation<Color>(Enumerable.Range(0, 10).Select(index =>
            new AnimationFrame<Color>(_normalColor.WithTransparency(_normalColor.A * (1f - index / 10f)),
                TimeSpan.FromMilliseconds(25)))));
    }

    private void StartStagger()
    {
        if (!StaggerAnimationEnabled || _spriteSheet is null) return;
        PlayAnimation(_spriteSheet.GetAnimation("Stagger"));
        _staggering = true;
    }

    /// <summary>Queues one tint for each atomic point of inflicted damage.</summary>
    private void ShowDamage(Combat.DamageTaken result)
    {
        var damage = result.Damage;
        if (result.Impact is null || _feedbackTextures is null) StartStagger();
        if (_damagePlayer.IsCompleted) _normalColor = _sprite.Color;
        var frames = new List<AnimationFrame<Color>>();
        foreach (var (type, amount) in damage.Typed)
        {
            var color = DamageColor(type);
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
        Loadout = Combat.CombatLoadout.FromCharacter(definition);
        Loadout.FocusChanged += () => ResourcesChanged?.Invoke(this);
        DamageReceiver = new(definition.Health);
        DamageReceiver.HealthChanged += () => ResourcesChanged?.Invoke(this);
        DamageReceiver.DamageTaken += ShowDamage;
        DamageReceiver.ImpactOccurred += ShowAttackEffect;
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
            _effectRenderer.IsVisible = false;
            _effectIconRenderer.IsVisible = false;
        };
        _damagePlayer = new(color => _sprite.Color = color);
        _damagePlayer.Completed += () =>
        {
            _sprite.Color = _normalColor;
            BeginDefeatAfterFeedback();
        };
        _effectPlayer = new(ApplyEffect);
        _effectPlayer.Completed += () =>
        {
            _effectRenderer.IsVisible = false;
            _effectIconRenderer.IsVisible = false;
            BeginDefeatAfterFeedback();
        };
        SpriteId = Renderer.Add(_sprite);
        AddComponent(Renderer);
        AddComponent(FocusRenderer);
        _effectRenderer.Add(_effectSprite);
        _effectIconRenderer.Add(_effectIcon);
        AddComponent(_effectRenderer);
        AddComponent(_effectIconRenderer);
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

        var elapsed = TimeSpan.FromSeconds(deltaTime * AnimationSpeed);
        _defeatPlayer.Advance(elapsed);
        _damagePlayer.Advance(elapsed);
        // Split at effect boundaries so a new stagger consumes only time after its cue,
        // including updates that span the entire effect.
        var remaining = elapsed;
        while (remaining > TimeSpan.Zero)
        {
            var step = _effectPlayer.IsCompleted ? remaining
                : (remaining < _effectPhaseRemaining ? remaining : _effectPhaseRemaining);
            _animationPlayer?.Advance(step);
            if ((_staggering || _attacking) && _animationPlayer is { IsCompleted: true })
            {
                _staggering = false; _attacking = false;
                ShowIdle();
            }
            if (!_effectPlayer.IsCompleted) _effectPhaseRemaining -= step;
            _effectPlayer.Advance(step);
            remaining -= step;
        }
        if (!_effectPlayer.IsCompleted)
        {
            var transform = Matrix4X4.CreateTranslation(0f, -VisualHeight * 0.55f - Elevation, 0f);
            _effectSprite.Transform = transform;
            _effectIcon.Transform = transform;
        }
        BeginDefeatAfterFeedback();
        base.Update(deltaTime);
    }
}






