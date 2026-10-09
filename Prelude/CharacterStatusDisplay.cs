namespace NexusRealms.Prelude;

/// <summary>Compact resource bars beside a character's overhead selection symbol.</summary>
public sealed class CharacterStatusDisplay : Element
{
    private readonly Character _character;
    private readonly ResourceRow _health;
    private readonly ResourceRow _focus;

    private readonly TextElement _eligibility;
    private readonly List<ImageElement> _symbols = [];
    public string Eligibility { get => _eligibility.Text; set => _eligibility.Text = value; }
    public CharacterStatusDisplay(Character character, ITexture stats, ITextStyle style, ITexture? statusIcons = null)
    {
        _character = character;
        _eligibility = new TextElement("", style) { Height = 20f, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI };
        Children.Add(_eligibility);
        _health = new ResourceRow(stats, "health-bar");
        _focus = new ResourceRow(stats, "focus-bar");
        Children.Add(_health);
        Children.Add(_focus);
        if (statusIcons is not null)
            foreach (var name in character.Definition.StatusSymbols)
            {
                var icon = new ImageElement
                {
                    Texture = statusIcons,
                    SourceRegion = statusIcons.GetRegion(name).Bounds,
                    SizingMode = ImageSizingMode.Fit,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI
                };
                _symbols.Add(icon); Children.Add(icon);
            }
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeRow(_health, _character.Health, 0);
        ArrangeRow(_focus, _character.Focus, 1);
        for (var i = 0; i < _symbols.Count; i++) _symbols[i].Arrange(new(Bounds.Origin.X + i * 20f, Bounds.Origin.Y + 66f, 18f, 18f));
        _eligibility.Arrange(new(Bounds.Origin.X, Bounds.Origin.Y + 44f, Bounds.Size.X, 20f));
    }

    private void ArrangeRow(ResourceRow row, int points, int index)
    {
        var y = Bounds.Origin.Y + index * 22f;
        row.SortOrder = SortOrder + 1;
        var available = Math.Max(0f, Bounds.Size.X);
        const float gap = -2f;
        var width = points > 0 ? MathF.Min(12f, available / points) : 12f;
        row.Arrange(new(Bounds.Origin.X, y + 3f, available, 14f));
        row.SetPoints(points, width, gap);
    }
}
