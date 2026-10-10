namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines immutable item data available to story nodes.
/// </summary>
public sealed record ItemData
{
    /// <summary>Alternative texture identifiers sharing the same item properties.</summary>
    public string[] Icons { get; init; } = [];
    public int IconIndex { get; init; }
    public string? Icon => ItemIconVariants.Select(Icons, IconIndex);
    public WeaponData? Weapon { get; init; }
}

public static class ItemIconVariants
{
    /// <summary>Wraps indices across the available variants, or returns no icon for an empty list.</summary>
    public static string? Select(string[]? icons, int index) => icons is { Length: > 0 }
        ? icons[((index % icons.Length) + icons.Length) % icons.Length] : null;
}
