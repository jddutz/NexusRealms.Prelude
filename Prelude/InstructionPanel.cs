namespace NexusRealms.Prelude;

using Nexus.Graphics.Geometry;

/// <summary>A dark instruction overlay with an atlas separator above its text.</summary>
public sealed class InstructionPanel : Element
{
    private readonly UniformColorMeshRenderer _background;
    private readonly List<IObservable> _ancestors = [];
    public TextElement Label { get; }

    public InstructionPanel(ITextStyle style)
    {
        _background = new UniformColorMeshRenderer
        {
            Mesh = new Mesh("prelude.instructions.trapezoid", PrimitiveTopologyEnum.TriangleList,
            [
                new(new(0f, 0f, 0f)), new(new(1f, 0f, 0f)), new(new(0.06f, 1f, 0f)),
                new(new(1f, 0f, 0f)), new(new(0.94f, 1f, 0f)), new(new(0.06f, 1f, 0f)),
            ]),
            Color = new Color(0f, 0f, 0f, 0.8f),
            IsVisible = false,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        AddComponent(_background);
        PropertyChanged += OnLayoutChanged;
        Label = new TextElement("Select a Target", style)
        {
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Center,
            Margins = new(8f, 8f, 2f, 2f),
            SortOrder = 1,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        Children.Add(Label);
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
        _background.Transform = Matrix4X4.CreateScale(Bounds.Size.X, Bounds.Size.Y, 1f)
            * Matrix4X4.CreateTranslation(Bounds.Origin.X, Bounds.Origin.Y, 0f);
        _background.DrawOrder = SortOrder;
        var visible = Bounds.Size.X > 0f && Bounds.Size.Y > 0f;
        for (ISceneNode? node = this; node is not null; node = node.Parent)
            if (node is IElement element && !element.IsVisible)
                visible = false;
        _background.IsVisible = visible;
    }
}
