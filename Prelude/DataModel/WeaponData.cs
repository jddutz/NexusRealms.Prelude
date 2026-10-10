namespace NexusRealms.Prelude.DataModel;

/// <summary>A weapon's complete typed damage composition and range.</summary>
public sealed record WeaponData
{
    public bool TwoHanded { get; init; }
    public bool IsMelee { get; init; } = true;
    public WeaponRange Range { get; init; } = WeaponRange.Short;

    public IReadOnlyDictionary<DamageType, int> Damage { get; init; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<DamageType, int>(new Dictionary<DamageType, int> { [DamageType.General] = 1 });
    /// <summary>Adds one extra Slashing point when this weapon already deals Slashing damage.</summary>
    public bool Sharp { get; init; }
}
