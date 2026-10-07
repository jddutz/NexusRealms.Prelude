namespace NexusRealms.Prelude;

/// <summary>Places the turn-order flow over a single continuous shadow.</summary>
public sealed class TurnOrderStrip : Element
{
    private readonly ImageElement _shadow = new()
    {
        Texture = BuiltInTextures.Uniform,
        Color = new Color(0f, 0f, 0f, 0.4f),
        SizingMode = ImageSizingMode.Stretch,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    public FlowLayout Items { get; } = new()
    {
        ItemSpacing = ItemSpacing.None,
        VerticalAlignment = AlignVertical.Top,
    };

    public TurnOrderStrip()
    {
        Children.Add(_shadow);
        Children.Add(Items);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        _shadow.SortOrder = SortOrder - 1;
        _shadow.Arrange(new(Bounds.Origin.X, Bounds.Origin.Y + 4f,
            Bounds.Size.X, MathF.Min(76f, Bounds.Size.Y)));
    }
}
