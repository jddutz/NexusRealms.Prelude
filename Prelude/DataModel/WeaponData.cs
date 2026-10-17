namespace NexusRealms.Prelude.DataModel;

/// <summary>A weapon's complete typed damage composition and range.</summary>
public enum AmmunitionType { Bullet, Arrow, Bolt, Stone }

public sealed record WeaponData
{
    /// <summary>Content ID of the damage effect selected by a basic attack using this item.</summary>
    public string ImpactTexture { get; init; } = "effects.impact_01.png";
    public AmmunitionType? Ammunition { get; init; }
    public int Shots { get; init; }
    public float ReloadTime { get; init; }
    public float AttackSpeed { get; init; } = 1f;
    public bool TwoHanded { get; init; }
    public bool IsMelee { get; init; } = true;
    public WeaponRange Range { get; init; } = WeaponRange.Short;

    public IReadOnlyDictionary<DamageType, int> Damage { get; init; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<DamageType, int>(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 });
    /// <summary>Adds one extra Slashing point when this weapon already deals Slashing damage.</summary>
    public bool Sharp { get; init; }
}
