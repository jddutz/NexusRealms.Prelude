namespace NexusRealms.Prelude;

/// <summary>A turn-order portrait with a lower diamond mask, atlas frame, and active marker.</summary>
public sealed class TurnOrderPortrait : Element
{
    private readonly ImageElement _portrait;
    private readonly ImageElement _topFrame;
    private readonly ImageElement _bottomFrame;
    private readonly ImageElement _marker;
    private readonly ITexture _atlas;
    private readonly ITexture _selectedAtlas;
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            var atlas = value ? _selectedAtlas : _atlas;
            _topFrame.Texture = atlas;
            _bottomFrame.Texture = atlas;
            _marker.Texture = atlas;
            _topFrame.SourceRegion = atlas.GetRegion(value ? "region-0013" : "region-0016").Bounds;
            _bottomFrame.SourceRegion = atlas.GetRegion(value ? "region-0018" : "region-0020").Bounds;
            _marker.SourceRegion = atlas.GetRegion("region-0004").Bounds;
            _marker.IsVisible = value;
            Arrange(Bounds);
            InvalidateLayout();
        }
    }

    public TurnOrderPortrait(ITexture portrait, ITexture atlas, ITexture selectedAtlas)
    {
        _atlas = atlas;
        _selectedAtlas = selectedAtlas;
        Width = 80f;
        Height = 96f;
        VerticalAlignment = AlignVertical.Top;
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
        Children.Add(_topFrame);
        Children.Add(_portrait);
        Children.Add(_bottomFrame);
        Children.Add(_marker);
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
        _topFrame.SortOrder = SortOrder + 2;
        _portrait.SortOrder = SortOrder + 3;
        _bottomFrame.SortOrder = SortOrder + 4;
        _marker.SortOrder = SortOrder + 6;
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
        var markerSize = MathF.Min(18f, side);
        var markerSource = _marker.SourceRegion!.Value.Size;
        var markerHeight = markerSize * markerSource.Y / markerSource.X;
        _marker.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - markerSize) * 0.5f,
            top + side + 2f, markerSize, markerHeight));
    }
}
