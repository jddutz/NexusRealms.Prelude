namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines a progression unlock; actions define combat behavior and requirements.
/// </summary>
public sealed record AbilityData
{
    /// <summary>
    /// Gets the ability's unique identifier.
    /// </summary>
    public required AbilityId Id { get; init; }

    /// <summary>Progression node display name.</summary>
    public required string Name { get; init; }

    /// <summary>Description of the progression unlock.</summary>
    public required string Description { get; init; }

    /// <summary>Capabilities made available for learning this ability, subject to their item requirements.</summary>
    public System.Collections.Immutable.ImmutableArray<ActionData> Actions { get; init; } = [];

    /// <summary>Progression presentation texture identifier.</summary>
    public required ContentId Icon { get; init; }
}
