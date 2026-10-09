namespace NexusRealms.Prelude;

/// <summary>Positions occurrences on the shared timeline over a single continuous shadow.</summary>
public sealed class TurnOrderStrip : Element
{
    private readonly InstructionPanel _instructions;
    private readonly ImageElement _separator;
    private readonly ImageElement _shadow = new()
    {

        SizingMode = ImageSizingMode.Stretch,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    private readonly List<(float Turn, Element Element)> _entries = [];
    private readonly List<ImageElement> _boundaries = [];
    private readonly ITexture _boundaryTexture;
    private float _currentTurn;

    public Element Items { get; } = new();

    public void SetOccurrences(float currentTurn, IEnumerable<(float Turn, Element Element)> entries)
    {
        _currentTurn = currentTurn;
        _entries.Clear();
        _entries.AddRange(entries);
        Items.Children.Clear();
        foreach (var marker in _boundaries) Children.Remove(marker);
        _boundaries.Clear();
        foreach (var entry in _entries) Items.Children.Add(entry.Element);
        var lastTurn = _entries.Count == 0 ? currentTurn : _entries.Max(x => x.Turn);
        // Integer positions strictly before the last occurrence, including the current
        // boundary when CurrentTurn is integral. A bounded preview also bounds marker work.
        for (double turn = Math.Ceiling(currentTurn); turn < lastTurn && _boundaries.Count < 256; turn++)
        {
            var marker = new ImageElement
            {
                Texture = _boundaryTexture,
                Width = 20f, Height = 20f,
                SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            _boundaries.Add(marker);
            Children.Add(marker);
        }
        InvalidateLayout();
    }
    public TurnOrderStrip(ITextureRegistry textures, ITextStyle style, ITexture shadow, ITexture instructionShadow)
    {
        _boundaryTexture = textures.GetOrCreate(new ContentId("ui.diamond.png"));
        _shadow.Texture = shadow;
        _instructions = new InstructionPanel(style, instructionShadow);
        _separator = new ImageElement
        {
            Texture = textures.GetOrCreate(new ContentId("ui.divider.png")),
            SizingMode = ImageSizingMode.Fit,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        Children.Add(_shadow);
        Children.Add(Items);
        Children.Add(_instructions);
        Children.Add(_separator);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        var lastTurn = _entries.Count == 0 ? _currentTurn : _entries.Max(x => x.Turn);
        var span = Math.Max(1d, (double)lastTurn - _currentTurn);
        // Reserve a separate slot for every occurrence and cap proportional
        // time spacing so a short queue stays grouped in the center.
        // Equal timestamps retain scheduler order.
        var itemWidth = MathF.Min(80f, Bounds.Size.X / Math.Max(1, _entries.Count));
        var timeWidth = Math.Min(24f, Math.Max(0f, Bounds.Size.X - itemWidth * _entries.Count));
        float TimeOffset(double turn) => (float)((turn - _currentTurn) / span) * timeWidth;
        var occupiedWidth = itemWidth * _entries.Count + TimeOffset(lastTurn);
        var timelineLeft = Bounds.Origin.X + (Bounds.Size.X - occupiedWidth) * 0.5f;
        for (var index = 0; index < _boundaries.Count; index++)
        {
            var turn = Math.Ceiling(_currentTurn) + index;
            var preceding = _entries.Count(entry => entry.Turn < turn);
            var left = timelineLeft + TimeOffset(turn) + preceding * itemWidth;
            var marker = _boundaries[index];
            marker.SortOrder = SortOrder;
            marker.Arrange(new(left - 10f, Bounds.Origin.Y + 28f, 20f, 20f));
        }
        for (var index = 0; index < _entries.Count; index++)
        {
            var entry = _entries[index];
            entry.Element.SortOrder = SortOrder +
                (entry.Element is TurnOrderPortrait { IsActive: true } ? 2 : 1);
            var left = timelineLeft + TimeOffset(entry.Turn) + index * itemWidth;
            entry.Element.Arrange(new(left, Bounds.Origin.Y, itemWidth, 96f));
        }
        _shadow.SortOrder = SortOrder - 1;
        _shadow.IsVisible = _entries.Count > 0;
        // Fit only the timeline contents. Instructions retain their independent bounds.
        var contentBounds = _entries.Select(entry => entry.Element.Bounds)
            .Concat(_boundaries.Select(marker => marker.Bounds)).ToArray();
        if (contentBounds.Length > 0)
        {
            var left = contentBounds.Min(rect => rect.Origin.X);
            var top = contentBounds.Min(rect => rect.Origin.Y);
            var right = contentBounds.Max(rect => rect.Origin.X + rect.Size.X);
            var bottom = contentBounds.Max(rect => rect.Origin.Y + rect.Size.Y);
            _shadow.Arrange(new(left, top, right - left, bottom - top));
        }
        // The instruction overlay extends below the grid's top row, like the marker.
        var separatorWidth = Bounds.Size.X * 0.18f;
        var texture = _separator.Texture!;
        var separatorHeight = separatorWidth * texture.Height / texture.Width;
        var separatorTop = Bounds.Origin.Y + 89f - separatorHeight * 0.5f;
        _separator.SortOrder = SortOrder + 3;
        _separator.Arrange(new(Bounds.Origin.X + (Bounds.Size.X - separatorWidth) * 0.5f,
            separatorTop, separatorWidth, separatorHeight));
        _instructions.SortOrder = SortOrder;
        _instructions.Label.SortOrder = SortOrder + 1;
        _instructions.Arrange(new(Bounds.Origin.X, Bounds.Origin.Y + 89f, Bounds.Size.X, 34f));
    }
}
