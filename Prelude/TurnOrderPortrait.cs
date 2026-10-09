namespace NexusRealms.Prelude;

/// <summary>A turn-order portrait with a lower diamond mask, standalone frame, and active marker.</summary>
public sealed class TurnOrderPortrait : Element
{
    private readonly ImageElement _portrait;
    private readonly ImageElement _topFrame;
    private readonly ImageElement _bottomFrame;
    private readonly ImageElement _marker;
    private readonly ITexture _topTexture;
    private readonly ITexture _bottomTexture;
    private readonly ITexture _selectedTopTexture;
    private readonly ITexture _selectedBottomTexture;
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            _topFrame.Texture = value ? _selectedTopTexture : _topTexture;
            _bottomFrame.Texture = value ? _selectedBottomTexture : _bottomTexture;
            _marker.IsVisible = value;
            Arrange(Bounds);
            InvalidateLayout();
        }
    }

    public TurnOrderPortrait(ITexture portrait, ITextureRegistry textures, bool isEnemy = false)
    {
        var prefix = isEnemy ? "enemy" : "character";
        _topTexture = textures.GetOrCreate(new ContentId($"ui.{prefix}_portrait_top.png"));
        _bottomTexture = textures.GetOrCreate(new ContentId($"ui.{prefix}_portrait_bottom.png"));
        _selectedTopTexture = isEnemy ? _topTexture : textures.GetOrCreate(new ContentId("ui.character_portrait_top_selected.png"));
        _selectedBottomTexture = isEnemy ? _bottomTexture : textures.GetOrCreate(new ContentId("ui.character_portrait_bottom_selected.png"));
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
        _topFrame = FrameImage(_topTexture);
        _bottomFrame = FrameImage(_bottomTexture);
        _marker = FrameImage(textures.GetOrCreate(new ContentId("ui.triangle.png")));
        _marker.IsVisible = false;
        Children.Add(_topFrame);
        Children.Add(_portrait);
        Children.Add(_bottomFrame);
        Children.Add(_marker);
    }

    private ImageElement FrameImage(ITexture texture) => new()
    {
        Texture = texture,
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
        var topSource = new Vector2D<float>(_topFrame.Texture!.Width, _topFrame.Texture.Height);
        var bottomSource = new Vector2D<float>(_bottomFrame.Texture!.Width, _bottomFrame.Texture.Height);
        var frameScale = side / bottomSource.X;
        var topWidth = topSource.X * frameScale;
        var topHeight = topSource.Y * frameScale;
        var bottomHeight = bottomSource.Y * frameScale;
        _topFrame.Arrange(new(left + (side - topWidth) * 0.5f, top, topWidth, topHeight));
        _bottomFrame.Arrange(new(left, top + side - bottomHeight, side, bottomHeight));
        var markerSize = MathF.Min(18f, side);
        var markerSource = new Vector2D<float>(_marker.Texture!.Width, _marker.Texture.Height);
        var markerHeight = markerSize * markerSource.Y / markerSource.X;
        _marker.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - markerSize) * 0.5f,
            top + side + 2f, markerSize, markerHeight));
    }
}
