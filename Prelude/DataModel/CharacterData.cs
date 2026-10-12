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
    public ActionData[] Attacks { get; init; } = [new()];
    /// <summary>General action capabilities; null uses the existing attack definitions.</summary>
    public ActionData[]? Actions { get; init; }
    /// <summary>Returns this character's capabilities, including compatibility attacks.</summary>
    public IReadOnlyList<ActionData> AvailableActions => Actions ?? Attacks;
    public int Focus { get; init; } = 2;

    /// <summary>
    /// Gets the identifier of the character's artwork.
    /// </summary>
    public required ContentId Artwork { get; init; }

    /// <summary>Gets the character's turn-order portrait texture.</summary>
    public required ContentId Portrait { get; init; }
}
