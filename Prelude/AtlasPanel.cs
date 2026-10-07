namespace NexusRealms.Prelude;

/// <summary>A layout panel whose atlas corners and edges retain their pixel sizes.</summary>
public sealed class AtlasPanel : Element
{
    private readonly NinePatchRenderer _renderer;
    private readonly List<IObservable> _ancestors = [];

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
        PropertyChanged += OnLayoutChanged;
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
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
