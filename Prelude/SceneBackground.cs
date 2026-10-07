namespace NexusRealms.Prelude;

/// <summary>Renders the combat background as a world-space sprite.</summary>
public sealed class SceneBackground : GameObject2D
{
    private readonly SpriteInstance _sprite;

    /// <summary>
    /// Initializes a world-space background from a texture and render-layer mask.
    /// </summary>
    /// <param name="texture">The background texture.</param>
    /// <param name="renderLayerMask">The layer on which the background is rendered.</param>
    public SceneBackground(ITexture texture, ulong renderLayerMask)
    {
        ArgumentNullException.ThrowIfNull(texture);

        Texture = texture;
        Renderer = new SpriteRenderer
        {
            Texture = texture,
            RenderLayerMask = renderLayerMask,
            DrawOrder = 0,
        };
        _sprite = new SpriteInstance
        {
            Size = new(texture.Width, texture.Height),
            Anchor = new(0.5f, 0.5f),
        };
        Renderer.Add(_sprite);
        AddComponent(Renderer);
    }

    /// <summary>Gets the source texture.</summary>
    public ITexture Texture { get; }

    /// <summary>Gets the renderer owned by this background.</summary>
    public SpriteRenderer Renderer { get; }

    /// <summary>
    /// Resizes and centers the background to fill the world viewport.
    /// </summary>
    /// <param name="worldSize">The world viewport dimensions.</param>
    public void SetWorldSize(Vector2D<float> worldSize)
    {
        if (
            !float.IsFinite(worldSize.X)
            || !float.IsFinite(worldSize.Y)
            || worldSize.X <= 0f
            || worldSize.Y <= 0f
        )
            throw new ArgumentOutOfRangeException(nameof(worldSize));

        Position = worldSize * 0.5f;
        Scale = new(worldSize.X / Texture.Width, worldSize.Y / Texture.Height);
    }
}
