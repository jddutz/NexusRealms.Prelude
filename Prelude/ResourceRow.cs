namespace NexusRealms.Prelude;

using Nexus.Graphics.Geometry;

/// <summary>An entire resource row rendered as instances of one parallelogram mesh.</summary>
public sealed class ResourceRow : Element
{
    private static readonly Mesh SegmentMesh = new("prelude.resource.parallelogram", PrimitiveTopologyEnum.TriangleList,
    [
        new(new(0.25f, 0f, 0f)), new(new(1f, 0f, 0f)), new(new(0f, 1f, 0f)),
        new(new(1f, 0f, 0f)), new(new(0.75f, 1f, 0f)), new(new(0f, 1f, 0f)),
    ]);
    private readonly UniformColorMeshRenderer _background;

    private readonly List<IObservable> _ancestors = [];
    private int _current;
    private float _width;
    private float _gap;
    private Color _color;

    public void SetPoints(int points, float width, float gap, Color color)
    {
        _current = Math.Max(0, points);
        _width = MathF.Max(0f, width);
        _gap = MathF.Max(0f, gap);
        _color = color;
        SynchronizeBackground();
    }

    public ResourceRow()
    {
        _background = new UniformColorMeshRenderer
        {
            Mesh = SegmentMesh,
            Color = new Color(0f, 0f, 0f, 0.8f),
            IsVisible = false,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        AddComponent(_background);
        PropertyChanged += OnLayoutChanged;
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        SynchronizeBackground();
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
        SynchronizeBackground();
    }

    private void OnLayoutChanged(string name)
    {
        if (name is "" or nameof(Bounds) or nameof(IsVisible) or nameof(SortOrder))
            SynchronizeBackground();
    }

    private void SynchronizeBackground()
    {
        var instances = new List<Nexus.Graphics.Drawables.UniformColorMeshInstance>();
        var width = _width;
        var height = Bounds.Size.Y;
        var inset = MathF.Min(0.75f, MathF.Min(width, height) * 0.12f);
        for (var point = 0; point < _current; point++)
        {
            var x = Bounds.Origin.X + point * (_width + _gap);
            var y = Bounds.Origin.Y;
            instances.Add(new(
                Matrix4X4.CreateScale(width, height, 1f) * Matrix4X4.CreateTranslation(x, y, 0f),
                new Color(0f, 0f, 0f, 1f)));
            instances.Add(new(
                Matrix4X4.CreateScale(MathF.Max(0f, width - 2f * inset), MathF.Max(0f, height - 2f * inset), 1f)
                    * Matrix4X4.CreateTranslation(x + inset, y + inset, 0f),
                _color));
        }
        _background.SetInstances(instances);
        _background.DrawOrder = SortOrder;
        var visible = Bounds.Size.X > 0f && Bounds.Size.Y > 0f;
        for (ISceneNode? node = this; node is not null; node = node.Parent)
            if (node is IElement element && !element.IsVisible)
                visible = false;
        _background.IsVisible = visible;

    }
}

