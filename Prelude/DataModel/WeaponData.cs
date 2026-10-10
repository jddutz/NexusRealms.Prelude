namespace NexusRealms.Prelude.DataModel;

/// <summary>Typed bonus points added to a Strike's one untyped base point.</summary>
public sealed record WeaponData
{
    public WeaponRange Range { get; init; } = WeaponRange.Short;

    public IReadOnlyDictionary<DamageType, int> BonusDamage { get; init; } = new Dictionary<DamageType, int>();
    /// <summary>Adds one extra Slashing point when this weapon already deals Slashing damage.</summary>
    public bool Sharp { get; init; }
}
