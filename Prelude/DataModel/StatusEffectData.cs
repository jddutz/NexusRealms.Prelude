namespace NexusRealms.Prelude.DataModel;

/// <summary>Presentation and explicit persistent behavior for a status identifier.</summary>
public sealed record StatusEffectData
{
    public required StatusEffectId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ContentId Icon { get; init; }
    /// <summary>Required duplicate policy for statuses used by the action resolver.</summary>
    public DuplicateStatusRule? Duplicates { get; init; }
    /// <summary>Default lifetime for initial status identifiers.</summary>
    public StatusPersistence Persistence { get; init; } = new();
    /// <summary>Resistance, negation, evasion, restriction, and modification rules while active.</summary>
    public System.Collections.Immutable.ImmutableArray<ActionEffect> Rules { get; init; } = [];
    /// <summary>Explicit metadata available to dispel selectors.</summary>
    public System.Collections.Immutable.ImmutableArray<string> Tags { get; init; } = [];
}
