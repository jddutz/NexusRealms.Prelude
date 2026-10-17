namespace NexusRealms.Prelude.Database;

/// <summary>Index of Zodiac ability definitions; records remain with their owning Zodiac.</summary>
public static class Abilities
{
    /// <summary>All registered definitions. Duplicate ability identifiers fail registration.</summary>
    public static FrozenDictionary<AbilityId, AbilityData> All { get; } =
        Progression.Aquarius.Definitions
            .Concat(Progression.PlaceholderAbilities.Definitions)
            .ToFrozenDictionary(ability => ability.Id);
}
