namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines an ability that can be used in combat.
/// </summary>
public sealed record AbilityData
{
    /// <summary>
    /// Gets the ability's unique identifier.
    /// </summary>
    public required AbilityId Id { get; init; }

    public float TurnCost { get; set; }

    public ContentId Icon { get; set; }
}
