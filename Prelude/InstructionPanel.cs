namespace NexusRealms.Prelude;

/// <summary>An instruction label over its own stretched shadow background.</summary>
public sealed class InstructionPanel : Element
{
    private readonly ImageElement _background;
    public TextElement Label { get; }

    public InstructionPanel(ITextStyle style, ITexture shadow)
    {
        _background = new ImageElement
        {
            Texture = shadow,
            SizingMode = ImageSizingMode.Stretch,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        Label = new TextElement("Choose an Action", style)
        {
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Center,
            Margins = new(8f, 8f, 2f, 2f),
            SortOrder = 1,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        Children.Add(_background);
        Children.Add(Label);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        _background.SortOrder = SortOrder;
        _background.Arrange(Bounds);
        Label.SortOrder = SortOrder + 1;
    }
}
