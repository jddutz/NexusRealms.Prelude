namespace NexusRealms.Prelude.DataModel;

/// <summary>Specific types assigned to individual damage points.</summary>
public enum DamageType
{
    Slashing,
    Piercing,
    Crushing,
}

public static class DamageTypeExtensions
{
    /// <summary>Gets the broad category of a specific damage type.</summary>
    public static DamageCategory GetCategory(this DamageType type) => type switch
    {
        DamageType.Slashing or DamageType.Piercing or DamageType.Crushing => DamageCategory.Physical,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown damage type."),
    };
}
