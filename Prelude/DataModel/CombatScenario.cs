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
    public int? RandomSeed { get; init; }

    public ImmutableArray<Combat.EncounterEvent> Events { get; init; } = [];

    public required ContentId Background { get; init; }

    /// <summary>
    /// Gets the immutable character placements in this encounter.
    /// </summary>
    public required ImmutableArray<CharacterPlacement> Characters { get; init; }
}


