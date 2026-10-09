namespace NexusRealms.Prelude;

/// <summary>Displays one fixed-size block for each player resource point.</summary>
public sealed class PlayerStatusBars : Element
{
    private const string HealthRegion = "health";
    private const string FocusRegion = "focus";
    private const float BlockWidth = 12f;
    private const float BlockGap = 2f;
    private const float BlockHeight = 14f;
    private const float IconSize = 22f;
    private const float RowHeight = IconSize;
    private const float StatusIconSize = 18f;
    private const float RowGap = 2f;
    private readonly GameState _state;
    private sealed record Row(ImageElement Icon, ResourceRow Segments, Color Color);
    private readonly Row _health;
    private readonly Row _focus;
    private readonly ImageElement[] _statusIcons;

    public PlayerStatusBars(GameState state, ITexture icons)
    {
        _state = state;
        Margins = new(100f, 80f, 6f, 6f);
        _health = CreateRow(icons, icons.GetRegion(HealthRegion).Bounds, new Color(0.75f, 0.12f, 0.1f));
        _focus = CreateRow(icons, icons.GetRegion(FocusRegion).Bounds, new Color(0.16f, 0.62f, 0.9f));
        // Select once per HUD so layout updates never reshuffle the effects.
        var statusRegions = icons.Regions
            .Where(region => region.Name != HealthRegion && region.Name != FocusRegion).ToArray();
        Random.Shared.Shuffle(statusRegions);
        _statusIcons = statusRegions.Take(3).Select(region => new ImageElement
        {
            Texture = icons,
            SourceRegion = region.Bounds,
            SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            SortOrder = 1,
        }).ToArray();
        foreach (var icon in _statusIcons)
            Children.Add(icon);
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
        var rowHeight = ResourceRowHeight;
        var statusY = Bounds.Origin.Y + rowHeight * 2f + RowGap + 4f;
        var remainingHeight = MathF.Max(0f, Bounds.Origin.Y + Bounds.Size.Y - statusY);
        var iconSize = MathF.Min(StatusIconSize, MathF.Max(0f, remainingHeight - 4f));
        var statusX = Bounds.Origin.X + (IconSize - iconSize) * 0.5f;
        for (var index = 0; index < _statusIcons.Length; index++)
        {
            var icon = _statusIcons[index];
            icon.IsVisible = iconSize > 0f && Bounds.Size.X > 0f;
            icon.Arrange(new(statusX + index * (iconSize + 4f), statusY, iconSize, iconSize));
        }
    }

    private float ResourceRowHeight => MathF.Min(RowHeight, MathF.Max(0f, (Bounds.Size.Y - RowGap) * 0.5f));

    private void ArrangeRow(Row row, int points, int index)
    {
        // Fixed rows at the top reserve the remaining panel height for status effects.
        var rowHeight = ResourceRowHeight;
        var y = Bounds.Origin.Y + index * (rowHeight + RowGap) + rowHeight * 0.5f;
        var iconSize = MathF.Min(IconSize, MathF.Min(rowHeight, Bounds.Size.X));
        row.Icon.IsVisible = iconSize > 0f;
        row.Icon.Arrange(new(Bounds.Origin.X, y - iconSize * 0.5f, iconSize, iconSize));
        var height = MathF.Min(BlockHeight, rowHeight);
        var count = Math.Max(0, points);
        var width = count > 0 ? count * (BlockWidth + BlockGap) - BlockGap : 0f;
        row.Segments.Arrange(new(Bounds.Origin.X + IconSize + 4f, y - height * 0.5f, width, height));
        row.Segments.SetPoints(count, BlockWidth, BlockGap, row.Color);
    }
}
