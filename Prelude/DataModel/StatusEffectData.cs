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
    /// <summary>Explicit metadata available to dispel selectors; use the negative tag for statuses affected by Cleanse.</summary>
    public System.Collections.Immutable.ImmutableArray<string> Tags { get; init; } = [];
    /// <summary>Optional temporary policy while this status remains active.</summary>
    public StrategyAssignment? StrategyOverride { get; init; }
    public int StrategyPriority { get; init; }
    /// <summary>Nonnegative elevation contributed while this status is active, independent of its name.</summary>
    public int Elevation { get; init; }
    /// <summary>Caster-only bonuses at successive whole timeline boundaries, in addition to normal +1 Focus.</summary>
    public ImmutableArray<int> FocusGainBonuses { get; init; } = [];
}
