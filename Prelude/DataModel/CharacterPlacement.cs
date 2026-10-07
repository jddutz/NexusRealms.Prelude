namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Assigns a character to a formation slot in a combat scenario.
/// </summary>
public sealed record CharacterPlacement
{
    /// <summary>
    /// Gets the identifier of the placed character.
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Gets the character's formation slot.
    /// </summary>
    public required FormationSlot Slot { get; init; }
}
