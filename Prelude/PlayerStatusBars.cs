namespace NexusRealms.Prelude;

/// <summary>Displays one fixed-size block for each player resource point.</summary>
public sealed class PlayerStatusBars : Element
{
    private const float BlockWidth = 14f;
    private const float BlockGap = -2f;
    private const float BlockHeight = 16f;
    private const float HealthRowOffset = 4f;
    private const float IconSize = 22f;
    private const float RowHeight = IconSize;
    private const float StatusIconSize = 18f;
    private const float RowGap = 2f;
    private readonly GameState _state;
    private sealed record Row(ImageElement Icon, ResourceRow Segments);
    private readonly Row _health;
    private readonly Row _focus;
    private readonly ImageElement[] _statusIcons;

    public PlayerStatusBars(GameState state, ITextureRegistry textures)
    {
        _state = state;
        // The portrait overlaps this panel by 29px; leave an 8px gap beside it.
        Margins = new(37f, 80f, 6f, 6f);
        _health = CreateRow(textures.GetOrCreate(new ContentId("stats.health_icon.png")),
            textures.GetOrCreate(new ContentId("stats.health_bar_segment.png")));
        _focus = CreateRow(textures.GetOrCreate(new ContentId("stats.focus_icon.png")),
            textures.GetOrCreate(new ContentId("stats.focus_bar_segment.png")));
        _statusIcons = state.StatusEffects.Select(id => new ImageElement
        {
            Texture = textures.GetOrCreate(StatusEffects.All[id].Icon),
            SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            SortOrder = 1,
        }).ToArray();
        foreach (var icon in _statusIcons)
            Children.Add(icon);
    }

    private Row CreateRow(ITexture iconTexture, ITexture segmentTexture)
    {
        var icon = new ImageElement
        {
            Texture = iconTexture, SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI, SortOrder = 1,
        };
        Children.Add(icon);
        var segments = new ResourceRow(segmentTexture) { SortOrder = 3 };
        Children.Add(segments);
        return new Row(icon, segments);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeRow(_health, _state.Health, 0);
        ArrangeRow(_focus, _state.Loadout.Focus, 1);
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
        var y = Bounds.Origin.Y + index * (rowHeight + RowGap) + rowHeight * 0.5f
            + (index == 0 ? HealthRowOffset : 0f);
        var iconSize = MathF.Min(IconSize, MathF.Min(rowHeight, Bounds.Size.X));
        row.Icon.IsVisible = iconSize > 0f;
        row.Icon.Arrange(new(Bounds.Origin.X, y - iconSize * 0.5f, iconSize, iconSize));
        var height = MathF.Min(BlockHeight, rowHeight);
        var count = Math.Max(0, points);
        var width = count > 0 ? count * (BlockWidth + BlockGap) - BlockGap : 0f;
        row.Segments.Arrange(new(Bounds.Origin.X + IconSize + 4f, y - height * 0.5f, width, height));
        row.Segments.SetPoints(count, BlockWidth, BlockGap);
    }
}
