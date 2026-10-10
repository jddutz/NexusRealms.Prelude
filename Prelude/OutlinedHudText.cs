namespace NexusRealms.Prelude;

/// <summary>Adds a one-pixel black outline to small HUD text.</summary>
public sealed class OutlinedHudText : TextElement
{
    private readonly TextElement[] _outline;
    private static readonly Vector2D<float>[] Offsets =
    [
        new(-1f, 0f),
        new(1f, 0f),
        new(0f, -1f),
        new(0f, 1f),
    ];

    public OutlinedHudText(ITextStyle style)
        : base("", style)
    {
        Color = UiTheme.TextColor;
        _outline = new TextElement[Offsets.Length];
        for (var i = 0; i < _outline.Length; i++)
        {
            _outline[i] = new TextElement("", style)
            {
                Color = Colors.Black,
                RenderLayerMask = RenderLayers.DefaultUI,
            };
            Children.Add(_outline[i]);
        }
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        for (var i = 0; i < _outline.Length; i++)
        {
            var outline = _outline[i];
            outline.Text = Text;
            outline.Style = Style;
            outline.HorizontalAlignment = HorizontalAlignment;
            outline.VerticalAlignment = VerticalAlignment;
            outline.SortOrder = SortOrder - 1;
            outline.Arrange(new(bounds.Origin + Offsets[i], bounds.Size));
        }
    }
}
