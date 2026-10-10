namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines immutable item data available to story nodes.
/// </summary>
public sealed record ItemData
{
    public WeaponData? Weapon { get; init; }
}
