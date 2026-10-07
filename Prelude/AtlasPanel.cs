namespace NexusRealms.Prelude;

/// <summary>A layout panel whose atlas corners and edges retain their pixel sizes.</summary>
public sealed class AtlasPanel : Element
{
    private static readonly ITexture SelectionGlowTexture = CreateSelectionGlowTexture();
    private readonly ImageElement _selectionGlow;
    private readonly NinePatchRenderer _renderer;
    private readonly List<IObservable> _ancestors = [];

    /// <summary>Shows a golden halo along the panel border when selected.</summary>
    public bool IsSelected
    {
        get => _selectionGlow.IsVisible;
        set => _selectionGlow.IsVisible = value;
    }

    public AtlasPanel(ITexture atlas, string regionName, Vector4D<float> sourceBorders)
    {
        var uv = atlas.GetRegion(regionName).TexCoords;
        _renderer = new NinePatchRenderer
        {
            IsVisible = false,
            Texture = atlas,
            TexCoord = new(uv.Origin.X, uv.Origin.Y, uv.Size.X, uv.Size.Y),
            SourceBorders = sourceBorders,
            BorderScale = 0.4f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        AddComponent(_renderer);
        _selectionGlow = new ImageElement
        {
            Texture = SelectionGlowTexture,
            Color = new Color(1f, 0.68f, 0.2f),
            IsVisible = false,
            SizingMode = ImageSizingMode.Stretch,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        Children.Add(_selectionGlow);
        PropertyChanged += OnLayoutChanged;
    }

    private static ITexture CreateSelectionGlowTexture()
    {
        const int size = 256;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = MathF.Abs((x + 0.5f) / size - 0.5f);
            var dy = MathF.Abs((y + 0.5f) / size - 0.5f);
            // The image extends beyond the panel, placing this ring just outside
            // its chamfered border. The panel covers the inner half of the halo.
            var distance = MathF.Max(MathF.Max(dx - 0.47f, dy - 0.47f),
                (dx + dy - 0.89f) / MathF.Sqrt(2f)) / 0.009f;
            var edgeFade = Math.Clamp((0.5f - MathF.Max(dx, dy)) / 0.015f, 0f, 1f);
            var alpha = 0.45f * MathF.Exp(-0.5f * distance * distance) * edgeFade;
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        return new Texture(new ContentId("prelude.panel.selection-glow"), size, size, pixels);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        _selectionGlow.SortOrder = SortOrder - 1;
        const float glowPadding = 5f;
        _selectionGlow.Arrange(new(Bounds.Origin.X - glowPadding, Bounds.Origin.Y - glowPadding,
            Bounds.Size.X + glowPadding * 2f, Bounds.Size.Y + glowPadding * 2f));
        SynchronizeRenderer();
    }

    public override void OnSceneHierarchyChanged()
    {
        base.OnSceneHierarchyChanged();
        foreach (var ancestor in _ancestors)
            ancestor.PropertyChanged -= OnLayoutChanged;
        _ancestors.Clear();
        for (ISceneNode? node = Parent; node is not null; node = node.Parent)
            if (node is IObservable observable)
            {
                observable.PropertyChanged += OnLayoutChanged;
                _ancestors.Add(observable);
            }
        SynchronizeRenderer();
    }

    private void OnLayoutChanged(string propertyName)
    {
        if (propertyName is "" or nameof(Bounds) or nameof(IsVisible))
            SynchronizeRenderer();
    }

    private void SynchronizeRenderer()
    {
        var visible = Bounds.Size.X > 0f && Bounds.Size.Y > 0f;
        for (ISceneNode? node = this; node is not null; node = node.Parent)
            if (node is IElement element && !element.IsVisible)
                visible = false;
        _renderer.IsVisible = false;
        _renderer.Destination = Bounds;
        _renderer.IsVisible = visible;
    }
}
