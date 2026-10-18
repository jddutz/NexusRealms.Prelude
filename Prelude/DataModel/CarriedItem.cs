namespace NexusRealms.Prelude.DataModel;

/// <summary>An owned item definition and quantity; mutable ownership and equipment belong to the combat loadout.</summary>
public sealed record CarriedItem(ItemId Id, string Name, string Slot, string[]? Icons = null, WeaponData? Weapon = null)
{
    /// <summary>Resource type supplied when this item is ammunition.</summary>
    public AmmunitionType? Ammunition { get; init; }
    /// <summary>Explicit equipment categories supplied by this item.</summary>
    public ImmutableArray<EquipmentKind> EquipmentKinds { get; init; } = [];
    /// <summary>Typed damage supplied when this item is thrown.</summary>
    public DamageProfile? ThrowDamage { get; init; }
    /// <summary>Positive number of units in this identity's stack.</summary>
    public int Quantity { get; init; } = 1;
    /// <summary>Selected visual variant.</summary>
    public int IconIndex { get; init; }
    /// <summary>The selected presentation texture identifier.</summary>
    public string? Icon => ItemIconVariants.Select(Icons, IconIndex);
}
