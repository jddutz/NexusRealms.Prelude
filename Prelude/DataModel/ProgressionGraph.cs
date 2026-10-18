namespace NexusRealms.Prelude.DataModel;

/// <summary>Authored ability definitions and placements for one Zodiac progression.</summary>
public sealed record ProgressionGraph
{
    /// <summary>The Zodiac sign that owns the progression.</summary>
    public ZodiacSignId Sign { get; }

    /// <summary>Ability placements and prerequisites relative to the Zodiac at (0, 0).</summary>
    public ImmutableArray<AbilityNode> Abilities { get; }

    /// <summary>The ability definitions owned by this progression.</summary>
    public ImmutableArray<AbilityData> AbilityDefinitions { get; }

    /// <summary>Ability definitions keyed within this progression only.</summary>
    public FrozenDictionary<AbilityId, AbilityData> AbilityDefinitionsById { get; }

    /// <summary>Creates a Zodiac progression with its own definitions and placements.</summary>
    /// <param name="sign">The owning Zodiac sign.</param>
    /// <param name="abilities">The placements and prerequisites within the graph.</param>
    /// <param name="abilityDefinitions">The ability data owned by this graph.</param>
    public ProgressionGraph(
        ZodiacSignId sign,
        ImmutableArray<AbilityNode> abilities,
        ImmutableArray<AbilityData> abilityDefinitions
    )
    {
        Sign = sign;
        Abilities = abilities;
        AbilityDefinitions = abilityDefinitions;
        AbilityDefinitionsById = abilityDefinitions.ToFrozenDictionary(ability => ability.Id);
    }
}
