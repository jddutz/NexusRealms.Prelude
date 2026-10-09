namespace NexusRealms.Prelude;

/// <summary>Displays one fixed-size block for each player resource point.</summary>
public sealed class PlayerStatusBars : Element
{
    private const float BlockWidth = 20f;
    private const float BlockGap = 2f;
    private readonly GameState _state;
    private sealed record Row(ImageElement Icon, ResourceRow Segments, Color Color);
    private readonly Row _health;
    private readonly Row _focus;

    public PlayerStatusBars(GameState state, ITexture icons)
    {
        _state = state;
        Margins = new(156f, 144f, 22f, 22f);
        _health = CreateRow(icons, new(60, 65, 248, 225), new Color(0.75f, 0.12f, 0.1f));
        _focus = CreateRow(icons, new(360, 30, 264, 285), new Color(0.65f, 0.85f, 1f));
    }

    private Row CreateRow(ITexture icons, Rectangle<int> source, Color color)
    {
        var icon = new ImageElement
        {
            Texture = icons, SourceRegion = source, SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI, SortOrder = 1,
        };
        Children.Add(icon);
        var segments = new ResourceRow { SortOrder = 3 };
        Children.Add(segments);
        return new Row(icon, segments, color);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeRow(_health, _state.Health, 0);
        ArrangeRow(_focus, _state.Focus, 1);
    }

    private void ArrangeRow(Row row, int points, int index)
    {
        var rowHeight = Bounds.Size.Y * 0.5f;
        var y = Bounds.Origin.Y + rowHeight * (index + 0.5f);
        var iconSize = MathF.Min(28f, MathF.Min(rowHeight, Bounds.Size.X));
        row.Icon.IsVisible = iconSize > 0f;
        row.Icon.Arrange(new(Bounds.Origin.X, y - iconSize * 0.5f, iconSize, iconSize));
        var height = MathF.Min(14f, rowHeight);
        var count = Math.Max(0, points);
        var width = count > 0 ? count * (BlockWidth + BlockGap) - BlockGap : 0f;
        row.Segments.Arrange(new(Bounds.Origin.X + 36f, y - height * 0.5f, width, height));
        row.Segments.SetPoints(count, BlockWidth, BlockGap, row.Color);
    }
}
