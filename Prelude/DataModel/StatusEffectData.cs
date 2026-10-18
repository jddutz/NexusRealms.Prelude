namespace NexusRealms.Prelude.DataModel;

/// <summary>Presentation and explicit persistent behavior for a status identifier.</summary>
public sealed record StatusEffectData
{
    /// <summary>Stable identity used by action effects and selectors.</summary>
    public required StatusEffectId Id { get; init; }
    /// <summary>Display name of the status.</summary>
    public required string Name { get; init; }
    /// <summary>Player-facing explanation of the status behavior.</summary>
    public required string Description { get; init; }
    /// <summary>Content identity for the status icon.</summary>
    public required ContentId Icon { get; init; }
    /// <summary>Required duplicate policy for statuses used by the action resolver.</summary>
    public DuplicateStatusRule? Duplicates { get; init; }
    /// <summary>Default lifetime for initial status identifiers.</summary>
    public StatusPersistence Persistence { get; init; } = new();
    /// <summary>Resistance, negation, evasion, restriction, and modification rules while active.</summary>
    public System.Collections.Immutable.ImmutableArray<ActionEffect> Rules { get; init; } = [];
    /// <summary>Ordered operations applied to the status owner at the start of each of their turns.</summary>
    public ImmutableArray<ActionEffect> TurnStartEffects { get; init; } = [];
    /// <summary>Operations triggered when the status owner deals qualifying damage.</summary>
    public DamageStatusTrigger? OnDamageDealt { get; init; }
    /// <summary>Operations triggered when the status owner takes qualifying damage.</summary>
    public DamageStatusTrigger? OnDamageTaken { get; init; }
    /// <summary>Explicit metadata available to dispel selectors; use the negative tag for statuses affected by Cleanse.</summary>
    public System.Collections.Immutable.ImmutableArray<string> Tags { get; init; } = [];
    /// <summary>Optional temporary policy while this status remains active.</summary>
    public StrategyAssignment? StrategyOverride { get; init; }
    /// <summary>Priority used when assigning a temporary strategy override.</summary>
    public int StrategyPriority { get; init; }
    /// <summary>Nonnegative elevation contributed while this status is active, independent of its name.</summary>
    public int Elevation { get; init; }
    /// <summary>Caster-only bonuses at successive whole timeline boundaries, in addition to normal +1 Focus.</summary>
    public ImmutableArray<int> FocusGainBonuses { get; init; } = [];
}

/// <summary>Typed operations triggered by a status when damage satisfies its filter.</summary>
public sealed record DamageStatusTrigger
{
    /// <summary>Creates a status trigger for damage matching the supplied filter.</summary>
    /// <param name="filter">Damage type, category, source, or action-affinity requirements.</param>
    /// <param name="effects">Ordered effects executed after qualifying damage is applied.</param>
    public DamageStatusTrigger(DamageFilter filter, ImmutableArray<ActionEffect> effects)
    {
        Filter = filter;
        Effects = effects;
    }

    /// <summary>Damage conditions required to activate this trigger.</summary>
    public DamageFilter Filter { get; init; }

    /// <summary>Ordered operations executed when this trigger activates.</summary>
    public ImmutableArray<ActionEffect> Effects { get; init; }
}
