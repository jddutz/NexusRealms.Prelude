namespace NexusRealms.Prelude;

/// <summary>A turn-order portrait with a lower diamond mask, atlas frame, and active marker.</summary>
public sealed class TurnOrderPortrait : Element
{
    private static readonly ITexture FrameGlowTexture = CreateGlowTexture(diamond: true);
    private static readonly ITexture MarkerGlowTexture = CreateGlowTexture(diamond: false);
    private readonly ImageElement _frameGlow;
    private readonly ImageElement _markerGlow;
    private readonly ImageElement _portrait;
    private readonly ImageElement _topFrame;
    private readonly ImageElement _bottomFrame;
    private readonly ImageElement _marker;
    private readonly ITexture _atlas;
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            _topFrame.SourceRegion = _atlas.GetRegion(value ? "region-0014" : "region-0016").Bounds;
            _bottomFrame.SourceRegion = _atlas.GetRegion(value ? "region-0018" : "region-0020").Bounds;
            _marker.IsVisible = value;
            _frameGlow.IsVisible = value;
            _markerGlow.IsVisible = value;
            Arrange(Bounds);
            InvalidateLayout();
        }
    }

    public TurnOrderPortrait(ITexture portrait, ITexture atlas)
    {
        _atlas = atlas;
        Width = 80f;
        Height = 96f;
        VerticalAlignment = AlignVertical.Top;
        _frameGlow = GlowImage(FrameGlowTexture);
        _markerGlow = GlowImage(MarkerGlowTexture);
        var crop = checked((int)Math.Min(portrait.Width, portrait.Height));
        _portrait = new ImageElement
        {
            Texture = portrait,
            SourceRegion = new(checked((int)portrait.Width - crop) / 2,
                checked((int)portrait.Height - crop) / 2, crop, crop),
            ClippingMask = ClippingMask.LowerHalfDiamond,
            ClippingInset = 0.06f,
            SizingMode = ImageSizingMode.Stretch,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        _topFrame = AtlasImage("region-0016");
        _bottomFrame = AtlasImage("region-0020");
        _marker = AtlasImage("region-0004");
        _marker.IsVisible = false;
        Children.Add(_frameGlow);
        Children.Add(_topFrame);
        Children.Add(_portrait);
        Children.Add(_bottomFrame);
        Children.Add(_markerGlow);
        Children.Add(_marker);
    }

    private static ImageElement GlowImage(ITexture texture) => new()
    {
        Texture = texture,
        Color = new Color(1f, 0.68f, 0.2f),
        IsVisible = false,
        SizingMode = ImageSizingMode.Stretch,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    private static ITexture CreateGlowTexture(bool diamond)
    {
        const int size = 128;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x + 0.5f) / size - 0.5f;
            var dy = (y + 0.5f) / size - 0.5f;
            // A soft ring follows the frame; the marker uses a compact halo.
            var distance = diamond
                ? (MathF.Abs(dx) + MathF.Abs(dy) - 0.41f) / 0.035f
                : MathF.Sqrt(dx * dx + dy * dy) / 0.18f;
            var alpha = (diamond ? 0.85f : 0.5f) * MathF.Exp(-0.5f * distance * distance);
            // Fade out at the image boundary to avoid a rectangular cutoff.
            var edgeFade = Math.Clamp((0.5f - MathF.Max(MathF.Abs(dx), MathF.Abs(dy))) / 0.06f, 0f, 1f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * edgeFade);
        }
        return new Texture(new ContentId(diamond ? "prelude.turn-order.frame-glow" : "prelude.turn-order.marker-glow"),
            size, size, pixels);
    }

    private ImageElement AtlasImage(string name) => new()
    {
        Texture = _atlas,
        SourceRegion = _atlas.GetRegion(name).Bounds,
        SizingMode = ImageSizingMode.Stretch,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        var side = MathF.Min(IsActive ? 76f : 64f, MathF.Min(Bounds.Size.X, Bounds.Size.Y));
        var left = Bounds.Origin.X + (Bounds.Size.X - side) * 0.5f;
        var top = Bounds.Origin.Y + (76f - side) * 0.5f;
        _frameGlow.SortOrder = SortOrder + 1;
        _topFrame.SortOrder = SortOrder + 2;
        _portrait.SortOrder = SortOrder + 3;
        _bottomFrame.SortOrder = SortOrder + 4;
        _markerGlow.SortOrder = SortOrder + 5;
        _marker.SortOrder = SortOrder + 6;
        _frameGlow.Arrange(new(left - 8f, top - 8f, side + 16f, side + 16f));
        _portrait.Arrange(new(left, top, side, side));
        // Both pieces use the same pixel scale. The lower piece includes the side
        // tips, so its bounds overlap the upper piece rather than starting halfway down.
        var topSource = _topFrame.SourceRegion!.Value.Size;
        var bottomSource = _bottomFrame.SourceRegion!.Value.Size;
        var frameScale = side / bottomSource.X;
        var topWidth = topSource.X * frameScale;
        var topHeight = topSource.Y * frameScale;
        var bottomHeight = bottomSource.Y * frameScale;
        _topFrame.Arrange(new(left + (side - topWidth) * 0.5f, top, topWidth, topHeight));
        _bottomFrame.Arrange(new(left, top + side - bottomHeight, side, bottomHeight));
        var markerSize = MathF.Min(16f, side);
        var markerSource = _marker.SourceRegion!.Value.Size;
        var markerHeight = markerSize * markerSource.Y / markerSource.X;
        _markerGlow.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - markerSize) * 0.5f - 5f,
            top + side - 3f, markerSize + 10f, markerHeight + 10f));
        _marker.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - markerSize) * 0.5f,
            top + side + 2f, markerSize, markerHeight));
    }
}
