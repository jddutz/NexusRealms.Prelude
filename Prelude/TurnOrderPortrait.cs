namespace NexusRealms.Prelude;

/// <summary>A turn-order portrait with a lower diamond mask, atlas frame, and active marker.</summary>
public sealed class TurnOrderPortrait : Element
{
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
            _topFrame.SourceRegion = _atlas.GetRegion(value ? "region-0010" : "region-0012").Bounds;
            _bottomFrame.SourceRegion = _atlas.GetRegion(value ? "region-0014" : "region-0016").Bounds;
            _marker.IsVisible = value;
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
        var crop = checked((int)Math.Min(portrait.Width, portrait.Height));
        _portrait = new ImageElement
        {
            Texture = portrait,
            SourceRegion = new(checked((int)portrait.Width - crop) / 2,
                checked((int)portrait.Height - crop) / 2, crop, crop),
            ClippingMask = ClippingMask.LowerHalfDiamond,
            ClippingInset = 0.04f,
            SizingMode = ImageSizingMode.Stretch,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        _topFrame = AtlasImage("region-0012");
        _bottomFrame = AtlasImage("region-0016");
        _marker = AtlasImage("region-0020");
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
        _topFrame.SortOrder = SortOrder;
        _portrait.SortOrder = SortOrder + 1;
        _bottomFrame.SortOrder = SortOrder + 2;
        _marker.SortOrder = SortOrder + 3;
        _portrait.Arrange(new(left, top, side, side));
        _topFrame.Arrange(new(left, top, side, side * 0.5f));
        _bottomFrame.Arrange(new(left, top + side * 0.5f, side, side * 0.5f));
        var markerSize = MathF.Min(16f, side);
        _marker.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - markerSize) * 0.5f,
            top + side + 2f, markerSize, markerSize));
    }
}
