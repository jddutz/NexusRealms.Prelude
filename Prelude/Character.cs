namespace NexusRealms.Prelude;

/// <summary>
/// Represents a combat character and owns the character's world-space visual.
/// </summary>
public sealed class Character : GameObject2D
{
    private readonly SpriteInstance _sprite;
    private SpriteInstance? _focusIndicator;
    private ITexture? _validIndicator;
    private ITexture? _invalidIndicator;
    private byte[]? _hitPixels;
    private int _health;
    private int _focus;
    public event Action<Character>? ResourcesChanged;
    public int Health
    {
        get => _health;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_health == value) return;
            _health = value;
            ResourcesChanged?.Invoke(this);
        }
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
    private float _elevation;

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
        Health = definition.Health;
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
        SpriteId = Renderer.Add(_sprite);
        AddComponent(Renderer);
        AddComponent(FocusRenderer);
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
        var height = Texture.Height * 0.12f;
        _focusIndicator.Size = new(height * texture.Width / texture.Height, height);
        _focusIndicator.Transform = Matrix4X4.CreateTranslation(0f,
            -Texture.Height - Elevation - Texture.Height * 0.02f, 0f);
        FocusRenderer.DrawOrder = 100;
        // Publish the drawable only after its selected texture and geometry are ready.
        FocusRenderer.IsVisible = IsFocused;
    }

    /// <summary>Tests the rendered sprite's opaque pixels rather than its transparent rectangle.</summary>
    public bool HitTest(Vector2D<float> worldPosition)
    {
        var x = (worldPosition.X - Position.X) / Scale.X + Texture.Width * 0.5f;
        var y = (worldPosition.Y - Position.Y) / Scale.Y + Texture.Height + Elevation;
        if (x < 0f || y < 0f || x >= Texture.Width || y >= Texture.Height) return false;
        if (_hitPixels is null)
        {
            _hitPixels = new byte[checked((int)Texture.Count * 4)];
            Texture.WriteTo(0, Texture.Count, ColorFormatEnum.RGBA8UNorm, _hitPixels);
        }
        return _hitPixels[((int)y * (int)Texture.Width + (int)x) * 4 + 3] > 16;
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

        _animationPlayer?.Advance(TimeSpan.FromSeconds(deltaTime));
        base.Update(deltaTime);
    }
}




