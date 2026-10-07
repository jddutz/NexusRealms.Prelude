namespace NexusRealms.Prelude.Combat;

/// <summary>
/// Represents a combat character and owns the character's world-space visual.
/// </summary>
public sealed class Character : GameObject2D
{
    private readonly SpriteInstance _sprite;
    private SpriteAnimationPlayer? _animationPlayer;
    private float _elevation;

    /// <summary>
    /// Initializes a character from its story definition and artwork texture.
    /// </summary>
    /// <param name="definition">The immutable story definition for the character.</param>
    /// <param name="texture">The texture used to render the character.</param>
    public Character(DataModel.Character definition, ITexture texture)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(texture);

        Definition = definition;
        Texture = texture;
        Renderer = new SpriteRenderer { Texture = texture };
        _sprite = new SpriteInstance
        {
            Size = new(texture.Width, texture.Height),
            Anchor = new(0.5f, 1f),
        };
        SpriteId = Renderer.Add(_sprite);
        AddComponent(Renderer);
    }

    /// <summary>Gets the immutable story definition represented by this entity.</summary>
    public DataModel.Character Definition { get; }

    /// <summary>Gets the texture used by the character's sprite.</summary>
    public ITexture Texture { get; }

    /// <summary>Gets the renderer owned by this character.</summary>
    public SpriteRenderer Renderer { get; }

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
