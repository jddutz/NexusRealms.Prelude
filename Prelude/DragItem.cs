namespace NexusRealms.Prelude;

/// <summary>A cursor-centered drag preview rendered directly in the UI layer, outside GUI layout.</summary>
public sealed class DragItem : GameObject2D
{
    private readonly IMouseInputDevice _mouse;

    public DragItem(ITexture texture, float maximumSize, IMouseInputDevice mouse)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(mouse);
        if (!float.IsFinite(maximumSize) || maximumSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(maximumSize));
        _mouse = mouse;
        var scale = maximumSize / Math.Max(texture.Width, texture.Height);
        Renderer = new SpriteRenderer
        {
            Texture = texture, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI, DrawOrder = 30000,
        };
        Renderer.Add(new SpriteInstance
        {
            Size = new(texture.Width * scale, texture.Height * scale), Anchor = new(0.5f, 0.5f),
        });
        AddComponent(Renderer);
        FollowMouse();
    }

    public SpriteRenderer Renderer { get; }

    public void FollowMouse()
    {
        Renderer.IsVisible = _mouse.IsConnected;
        if (_mouse.IsConnected) Position = _mouse.Position;
    }

    public override void Update(double deltaTime)
    {
        base.Update(deltaTime);
        FollowMouse();
    }
}
