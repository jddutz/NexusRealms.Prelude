namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Assigns a character to a formation slot in a combat scenario.
/// </summary>
public sealed record CharacterPlacement
{
    /// <summary>
    /// Gets the identifier of the placed character.
    /// </summary>
    public Combat.CombatTeam Team { get; init; } = Combat.CombatTeam.Enemies;
    /// <summary>Defeating this character is one of the scenario’s required victory conditions.</summary>
    public bool RequiredForVictory { get; init; }
    public float InitialTurn { get; init; }

    /// <summary>Scenario-specific priority used to break ties at the same turn.</summary>
    public int Initiative { get; init; }

    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Gets the character's formation slot.
    /// </summary>
    public required FormationSlot Slot { get; init; }
}


