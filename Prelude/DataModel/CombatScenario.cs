using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines a combat encounter, its background, and the characters assigned to formation slots.
/// </summary>
public sealed record CombatScenario : StoryNode
{
    /// <summary>
    /// Gets the identifier of the encounter background.
    /// </summary>
    /// <summary>Optional replay seed; otherwise each encounter receives a fresh seed.</summary>
    public StoryNodeId? VictoryNode { get; init; }
    public StoryNodeId? DefeatNode { get; init; }
    public StoryNodeId? EscapeNode { get; init; }
    public ImmutableArray<Func<Combat.CombatSystem, bool>> VictoryConditions { get; init; } = [];
    public ImmutableArray<Func<Combat.CombatSystem, bool>> DefeatConditions { get; init; } = [Combat.CombatConditions.PlayerDefeated];
    public int? RandomSeed { get; init; }

    /// <summary>Scenario-specific initiative for the player, who has no formation placement.</summary>
    public int PlayerInitiative { get; init; }

    public ImmutableArray<Combat.EncounterEvent> Events { get; init; } = [];

    public required ContentId Background { get; init; }

    /// <summary>
    /// Gets the immutable character placements in this encounter.
    /// </summary>
    public required ImmutableArray<CharacterPlacement> Characters { get; init; }

    /// <summary>Combines explicit objectives and designated characters into the encounter’s predicates.</summary>
    public Func<Combat.CombatSystem, bool>[] BuildVictoryConditions()
    {
        var conditions = VictoryConditions.Concat(Characters.Select((placement, index) => (placement, index))
            .Where(entry => entry.placement.RequiredForVictory)
            .Select(entry => Combat.CombatConditions.CharacterDefeated($"encounter-{entry.index}"))).ToArray();
        return conditions.Length > 0 ? conditions : [Combat.CombatConditions.AllEnemiesDefeated];
    }
}


