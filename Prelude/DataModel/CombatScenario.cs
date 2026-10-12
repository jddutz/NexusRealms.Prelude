using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>Authored encounter data; runtime predicate construction belongs to ScenarioRules.</summary>
public sealed record CombatScenario : StoryNode
{
    /// <summary>Destination after victory.</summary>
    public StoryNodeId? VictoryNode { get; init; }
    /// <summary>Destination after defeat.</summary>
    public StoryNodeId? DefeatNode { get; init; }
    /// <summary>Destination after escape.</summary>
    public StoryNodeId? EscapeNode { get; init; }
    /// <summary>All explicit objectives required for victory, in addition to designated characters.</summary>
    public ImmutableArray<CombatConditionData> VictoryConditions { get; init; } = [];
    /// <summary>Any condition which ends the encounter in defeat.</summary>
    public ImmutableArray<CombatConditionData> DefeatConditions { get; init; } = [new(CombatConditionKind.PlayerDefeated)];
    /// <summary>Optional deterministic encounter seed.</summary>
    public int? RandomSeed { get; init; }
    /// <summary>Player priority for equal-time occurrences.</summary>
    public int PlayerInitiative { get; init; }
    /// <summary>Authored timeline effects.</summary>
    public ImmutableArray<EncounterEventData> Events { get; init; } = [];
    /// <summary>Background content identifier.</summary>
    public required ContentId Background { get; init; }
    /// <summary>Authored non-player formation placements.</summary>
    public required ImmutableArray<CharacterPlacement> Characters { get; init; }
}
/// <summary>Supported data-only encounter objective kinds.</summary>
public enum CombatConditionKind { PlayerDefeated, AllEnemiesDefeated, CharacterDefeated }
/// <summary>A typed objective using a scheduler character identity where applicable.</summary>
public sealed record CombatConditionData(CombatConditionKind Kind, string? CharacterId = null);
/// <summary>An authored occurrence executing effects without a character commitment.</summary>
public sealed record EncounterEventData(float Turn, string Label, string SourceId, string RecipientId, ActionData Action,
    int Priority = 0, string? Icon = null, bool IsVisible = true, string? PresentationKey = null)
{
    /// <summary>AI currently supports fully visible encounters; unrevealed rules require an observation view.</summary>
    public bool KnownToAi { get; init; } = true;
}
