namespace NexusRealms.Prelude;

/// <summary>A resource row rendered with one standalone segment texture per point.</summary>
public sealed class ResourceRow : Element
{
    private readonly ITexture _texture;
    private int _current;
    private float _width;
    private float _gap;

    public ResourceRow(ITexture texture)
    {
        _texture = texture;
    }

    public void SetPoints(int points, float width, float gap)
    {
        _current = Math.Max(0, points);
        _width = MathF.Max(0f, width);
        // Segment textures contain transparent margins; overlap them slightly to
        // bring the visible artwork closer while retaining a positive stride.
        _gap = MathF.Max(-_width + 1f, gap);
        while (Children.Count > _current)
            Children.RemoveAt(Children.Count - 1);
        while (Children.Count < _current)
            Children.Add(new ImageElement
            {
                Texture = _texture,
                SizingMode = ImageSizingMode.Stretch,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
        ArrangeSegments();
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeSegments();
    }

    private void ArrangeSegments()
    {
        for (var point = 0; point < Children.Count; point++)
        {
            var segment = (ImageElement)Children[point];
            segment.SortOrder = SortOrder;
            segment.Arrange(new(Bounds.Origin.X + point * (_width + _gap),
                Bounds.Origin.Y, _width, Bounds.Size.Y));
        }
    }
}
