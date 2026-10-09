namespace NexusRealms.Prelude;

/// <summary>Compact screen-space resources beside a character's overhead selection symbol.</summary>
public sealed class CharacterStatusDisplay : Element
{
    private readonly Character _character;
    private readonly ImageElement _healthIcon;
    private readonly ImageElement _focusIcon;
    private readonly ResourceRow _health = new();
    private readonly ResourceRow _focus = new();

    public CharacterStatusDisplay(Character character, ITexture icons)
    {
        _character = character;
        _healthIcon = CreateIcon(icons, "health");
        _focusIcon = CreateIcon(icons, "focus");
        Children.Add(_healthIcon);
        Children.Add(_focusIcon);
        Children.Add(_health);
        Children.Add(_focus);
    }

    private static ImageElement CreateIcon(ITexture texture, string region) => new()
    {
        Texture = texture,
        SourceRegion = texture.GetRegion(region).Bounds,
        SizingMode = ImageSizingMode.Fit,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeRow(_healthIcon, _health, _character.Health, 0, new(0.75f, 0.12f, 0.1f));
        ArrangeRow(_focusIcon, _focus, _character.Focus, 1, new(0.16f, 0.62f, 0.9f));
    }

    private void ArrangeRow(ImageElement icon, ResourceRow row, int points, int index, Color color)
    {
        var y = Bounds.Origin.Y + index * 22f;
        icon.SortOrder = SortOrder;
        row.SortOrder = SortOrder + 1;
        icon.Arrange(new(Bounds.Origin.X, y, 20f, 20f));
        var available = Math.Max(0f, Bounds.Size.X - 26f);
        var gap = points > 1 ? MathF.Min(2f, available / (points * 2f)) : 0f;
        var width = points > 0 ? MathF.Min(12f, Math.Max(0f,
            (available - (points - 1) * gap) / points)) : 12f;
        row.Arrange(new(Bounds.Origin.X + 26f, y + 3f, available, 14f));
        row.SetPoints(points, width, gap, color);
    }
}
