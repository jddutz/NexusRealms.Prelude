namespace NexusRealms.Prelude;

/// <summary>Displays player resources with a shared, space-limited scale.</summary>
public sealed class PlayerStatusBars : Element
{
    private readonly GameState _state;
    private sealed record Row(ImageElement Icon, ImageElement Border, ImageElement Track, ImageElement Fill, TextElement Number);
    private readonly Row _health;
    private readonly Row _focus;

    public PlayerStatusBars(GameState state, ITexture icons, ITextStyle style)
    {
        _state = state;
        Margins = new(156f, 144f, 22f, 22f);
        _health = CreateRow(icons, new(60, 65, 248, 225), new Color(0.75f, 0.12f, 0.1f), style);
        _focus = CreateRow(icons, new(360, 30, 264, 285), new Color(0.65f, 0.85f, 1f), style);
    }

    private Row CreateRow(
        ITexture icons, Rectangle<int> source, Color color, ITextStyle style)
    {
        var icon = new ImageElement
        {
            Texture = icons, SourceRegion = source, SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI, SortOrder = 1,
        };
        var border = SolidImage(Colors.Black, 1);
        var track = SolidImage(new Color(0.12f, 0.12f, 0.12f), 2);
        var fill = SolidImage(color, 3);
        var number = new OutlinedHudText(style)
        {
            HorizontalAlignment = AlignHorizontal.Left,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI, SortOrder = 4,
        };
        Children.Add(icon);
        Children.Add(border);
        Children.Add(track);
        Children.Add(fill);
        Children.Add(number);
        return new Row(icon, border, track, fill, number);
    }

    private static ImageElement SolidImage(Color color, int order) => new()
    {
        Texture = BuiltInTextures.Uniform, Color = color, SortOrder = order,
        SizingMode = ImageSizingMode.Stretch,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        const float fixedWidth = 28f + 8f + 8f + 48f;
        var availableBarWidth = MathF.Max(0f, Bounds.Size.X - fixedWidth);
        var largestMaximum = Math.Max(1, Math.Max(_state.MaximumHealth, _state.MaximumFocus));
        var pixelsPerPoint = MathF.Min(2f, availableBarWidth / largestMaximum);
        ArrangeRow(_health, _state.Health, _state.MaximumHealth, 0, pixelsPerPoint);
        ArrangeRow(_focus, _state.Focus, _state.MaximumFocus, 1, pixelsPerPoint);
    }

    private void ArrangeRow(
        Row row,
        int current, int maximum, int index, float scale)
    {
        var rowHeight = Bounds.Size.Y * 0.5f;
        var y = Bounds.Origin.Y + rowHeight * (index + 0.5f);
        var iconSize = MathF.Min(28f, MathF.Min(rowHeight, Bounds.Size.X));
        ArrangeImage(row.Icon, new(Bounds.Origin.X, y - iconSize * 0.5f, iconSize, iconSize));
        var x = Bounds.Origin.X + 36f;
        var width = Math.Max(0, maximum) * scale;
        var barHeight = MathF.Min(14f, rowHeight);
        ArrangeImage(row.Border, new(x, y - barHeight * 0.5f, width, barHeight));
        var borderSize = MathF.Min(1f, MathF.Min(width, barHeight) * 0.5f);
        var innerWidth = MathF.Max(0f, width - borderSize * 2f);
        var innerHeight = MathF.Max(0f, barHeight - borderSize * 2f);
        ArrangeImage(row.Track, new(x + borderSize, y - innerHeight * 0.5f, innerWidth, innerHeight));
        var fraction = maximum > 0 ? Math.Clamp((float)current / maximum, 0f, 1f) : 0f;
        ArrangeImage(row.Fill, new(x + borderSize, y - innerHeight * 0.5f, innerWidth * fraction, innerHeight));
        row.Number.Text = Math.Max(0, current).ToString();
        var numberX = MathF.Min(x + width + 8f, Bounds.Origin.X + Bounds.Size.X);
        var numberWidth = MathF.Min(48f, MathF.Max(0f, Bounds.Origin.X + Bounds.Size.X - numberX));
        row.Number.IsVisible = numberWidth > 0f && rowHeight > 0f;
        row.Number.Arrange(new(numberX, y - rowHeight * 0.5f, numberWidth, rowHeight));
    }

    private static void ArrangeImage(ImageElement image, Rectangle<float> bounds)
    {
        image.IsVisible = bounds.Size.X > 0f && bounds.Size.Y > 0f;
        image.Arrange(bounds);
    }
}
