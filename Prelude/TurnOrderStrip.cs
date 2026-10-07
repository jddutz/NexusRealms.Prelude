namespace NexusRealms.Prelude;

/// <summary>Places the turn-order flow over a single continuous shadow.</summary>
public sealed class TurnOrderStrip : Element
{
    private readonly InstructionPanel _instructions;
    private readonly ImageElement _separator;
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

    public TurnOrderStrip(ITexture atlas, ITextStyle style)
    {
        _instructions = new InstructionPanel(style);
        _separator = new ImageElement
        {
            Texture = atlas,
            SourceRegion = atlas.GetRegion("region-0022").Bounds,
            SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            SortOrder = 1,
        };
        Children.Add(_shadow);
        Children.Add(Items);
        Children.Add(_instructions);
        Children.Add(_separator);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        _shadow.SortOrder = SortOrder - 1;
        _shadow.Arrange(new(Bounds.Origin.X, Bounds.Origin.Y + 4f,
            Bounds.Size.X, MathF.Min(76f, Bounds.Size.Y)));
        // The instruction overlay extends below the grid's top row, like the marker.
        var separatorWidth = Bounds.Size.X * 0.36f;
        var sourceSize = _separator.SourceRegion!.Value.Size;
        var separatorHeight = separatorWidth * sourceSize.Y / sourceSize.X;
        var separatorTop = Bounds.Origin.Y + 89f - separatorHeight * 0.5f;
        _instructions.Arrange(new(Bounds.Origin.X, separatorTop + separatorHeight * 0.5f,
            Bounds.Size.X, 34f));
        _separator.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - separatorWidth) * 0.5f,
            separatorTop, separatorWidth, separatorHeight));
    }
}
