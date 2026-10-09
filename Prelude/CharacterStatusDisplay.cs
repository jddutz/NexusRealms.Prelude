namespace NexusRealms.Prelude;

/// <summary>Compact resource bars beside a character's overhead selection symbol.</summary>
public sealed class CharacterStatusDisplay : Element
{
    private readonly Character _character;
    private readonly ResourceRow _health = new();
    private readonly ResourceRow _focus = new();

    public CharacterStatusDisplay(Character character)
    {
        _character = character;
        Children.Add(_health);
        Children.Add(_focus);
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeRow(_health, _character.Health, 0, new(0.75f, 0.12f, 0.1f));
        ArrangeRow(_focus, _character.Focus, 1, new(0.16f, 0.62f, 0.9f));
    }

    private void ArrangeRow(ResourceRow row, int points, int index, Color color)
    {
        var y = Bounds.Origin.Y + index * 22f;
        row.SortOrder = SortOrder + 1;
        var available = Math.Max(0f, Bounds.Size.X);
        const float gap = 0f;
        var width = points > 0 ? MathF.Min(12f, available / points) : 12f;
        row.Arrange(new(Bounds.Origin.X, y + 3f, available, 14f));
        row.SetPoints(points, width, gap, color);
    }
}
