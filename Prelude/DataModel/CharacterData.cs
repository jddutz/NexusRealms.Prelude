namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines an immutable character available to story nodes.
/// </summary>
public sealed record CharacterData
{
    /// <summary>
    /// Gets the character's unique identifier.
    /// </summary>
    public required CharacterId Id { get; init; }

    /// <summary>
    /// Gets the character's display name.
    /// </summary>
    public required string Name { get; init; }
    public StatusEffectId[] StatusEffects { get; init; } = [];
    public int Health { get; init; } = 5;
    public CarriedItem[] Items { get; init; } = [];
    /// <summary>Explicit initial equipment associations; remaining items begin in inventory.</summary>
    public System.Collections.Immutable.ImmutableDictionary<string, string> Equipment { get; init; }
        = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
    /// <summary>Complete set of this character's authored action capabilities.</summary>
    public ActionData[] Actions { get; init; } = [new()];
    public int Focus { get; init; } = 2;
    /// <summary>Explicitly add basic hand, Throw, and Wait commands to this character's authored actions.</summary>
    public bool BasicCommands { get; init; }

    /// <summary>This character's reusable decision policy, independent of scenario placement.</summary>
    public StrategyAssignment Strategy { get; init; } = new();
    /// <summary>Character-owned conditions for changing its runtime policy.</summary>
    public ImmutableArray<StrategyTransition> StrategyTransitions { get; init; } = [];

    /// <summary>
    /// Gets the identifier of the character's artwork.
    /// </summary>
    public required ContentId Artwork { get; init; }

    /// <summary>Gets the character's turn-order portrait texture.</summary>
    public required ContentId Portrait { get; init; }
}
