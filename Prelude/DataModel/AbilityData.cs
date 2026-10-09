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

    public required string Name { get; init; }

    public required string Description { get; init; }

    public float TurnCost { get; set; }

    public required ContentId Icon { get; init; }
}
