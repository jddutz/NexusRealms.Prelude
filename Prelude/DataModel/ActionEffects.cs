using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>The fifteen operation names; targeting and costs are separate concerns.</summary>
public enum EffectVerb { Damage, Restore, Delay, Resist, Negate, Evade, Move, Restrict, Modify, Consume, Grant, Remove, Apply, Dispel, Transfer }
/// <summary>Supported non-Health resources.</summary>
public enum ResourceKind { Focus }
/// <summary>Numerical fields explicitly supported by additive modification.</summary>
public enum NumericalField { DamageAmount, RestoreAmount, DelayAmount, StatusDuration }
/// <summary>How multiple criteria in a filter are combined.</summary>
public enum FilterMatch { All, Any }
/// <summary>How an operation chooses recipients from the frozen action target set.</summary>
public enum RecipientScope { Targets, Source }
/// <summary>Whether an effect changed state or was prevented.</summary>
public enum EffectResult { Applied, Reduced, Negated, Evaded, Unchanged }
/// <summary>How duplicate statuses are handled.</summary>
public enum DuplicateStatusRule { Reject, Refresh, Replace, Stack }
/// <summary>Explicit clocks and expiration boundaries for a status.</summary>
public enum StatusClock { Permanent, Timeline, RecipientTurnStart }
/// <summary>Which qualifying incoming event consumes a status use.</summary>
public enum StatusUseEvent { Never, IncomingAction, NonEvadedIncomingAction }

/// <summary>Matches explicit action identity, classification, and source; empty means all.</summary>
public sealed record ActionFilter(string? ActionId = null, string? Classification = null, string? SourceId = null, FilterMatch Match = FilterMatch.All, ResourceKind? SpendsResource = null, TargetingFlags? RequiresTargetingFlags = null)
{
    /// <summary>Evaluates only declared criteria using the authored combination rule.</summary>
    public bool Matches(ActionData action, string source)
    {
        var tests = new List<bool>();
        if (ActionId is not null) tests.Add(action.Id == ActionId);
        if (Classification is not null) tests.Add(action.Classifications.Contains(Classification));
        if (SourceId is not null) tests.Add(source == SourceId);
        if (RequiresTargetingFlags is { } flags) tests.Add((action.Targeting.Flags & flags) == flags);
        if (SpendsResource is { } resource)
            tests.Add(action.Costs.Any(cost => cost is { Subject: ResourceSubject subject, Amount: > 0 } && subject.Resource == resource)
                || action.BuildEffects().Any(effect => effect.Recipients == RecipientScope.Source && (effect switch
                {
                    ConsumeEffect { Subject: ResourceSubject subject, Amount: > 0 } => subject.Resource == resource,
                    TransferEffect { Subject: ResourceSubject subject, Amount: > 0 } => subject.Resource == resource,
                    _ => false,
                })));
        return tests.Count == 0 || (Match == FilterMatch.All ? tests.All(value => value) : tests.Any(value => value));
    }
}
/// <summary>Source and damage-type criteria default to both matching.</summary>
public sealed record DamageFilter(DamageType? Type = null, string? SourceId = null, FilterMatch Match = FilterMatch.All, DamageCategory? Category = null)
{
    /// <summary>Tests a typed damage amount and its source.</summary>
    public bool Matches(DamageType type, string source)
    {
        var tests = new List<bool>();
        if (Type is { } expected) tests.Add(type == expected);
        if (SourceId is not null) tests.Add(source == SourceId);
        if (Category is { } category) tests.Add(type.GetCategory() == category);
        return tests.Count == 0 || (Match == FilterMatch.All ? tests.All(value => value) : tests.Any(value => value));
    }
}
/// <summary>Matches an operation and, optionally, a damage type.</summary>
public sealed record EffectFilter(EffectVerb Verb, DamageType? DamageType = null);
/// <summary>Explicit status persistence; duration units are determined by Clock.</summary>
public sealed record StatusPersistence(StatusClock Clock = StatusClock.Permanent, float Duration = 0, int? Uses = null,
    StatusUseEvent UseEvent = StatusUseEvent.Never, ActionFilter? UseFilter = null);
/// <summary>A status selector; limited dispels always use application sequence order.</summary>
public sealed record StatusSelector(StatusEffectId? Id = null, string? Tag = null);
/// <summary>A typed subject consumed from a quantity.</summary>
public abstract record ConsumableSubject;
/// <summary>A declared non-Health resource.</summary>
public sealed record ResourceSubject(ResourceKind Resource) : ConsumableSubject;
/// <summary>An owned item stack identified independently of equipment.</summary>
public sealed record ItemQuantitySubject(ItemId Item) : ConsumableSubject;
/// <summary>A subject which may be granted.</summary>
public abstract record GrantSubject;
/// <summary>Additional Focus or another supported resource.</summary>
public sealed record GrantResource(ResourceKind Resource, int Amount) : GrantSubject;
/// <summary>A new owned item; never automatically equipped.</summary>
public sealed record GrantItem(CarriedItem Item) : GrantSubject;
/// <summary>A character capability; this does not learn an ability.</summary>
public sealed record GrantCapability(string ActionId) : GrantSubject;
/// <summary>An entity or association which may be removed.</summary>
public abstract record RemoveSubject;
/// <summary>Removes an owned item and its equipment references.</summary>
public sealed record RemoveItem(ItemId Item) : RemoveSubject;
/// <summary>Unequips one item without destroying it.</summary>
public sealed record RemoveEquipment(string Slot) : RemoveSubject;
/// <summary>Removes an available capability without altering progression.</summary>
public sealed record RemoveCapability(string ActionId) : RemoveSubject;

/// <summary>Base for strongly typed operations in authored resolution order.</summary>
public abstract record ActionEffect
{
    /// <summary>Operation identity in structured results; defaults to authored list index.</summary>
    public string? Id { get; init; }
    /// <summary>Source effects remain independent of target evasion, including execution commitments.</summary>
    public RecipientScope Recipients { get; init; } = RecipientScope.Targets;
    /// <summary>Optional earlier effect index that must have changed this recipient's state.</summary>
    public int? RequiresEffect { get; init; }
    /// <summary>This operation requires the recipient to touch the ground, independent of other operations.</summary>
    public bool RequiresGroundContact { get; init; }
    /// <summary>Independent attack within an action; resistance budgets are separate per attack.</summary>
    public int AttackIndex { get; init; }
    /// <summary>Reach of this operation when an action combines attacks with different weapon ranges.</summary>
    public WeaponRange? MeleeRange { get; init; }
    /// <summary>The operation name.</summary>
    public abstract EffectVerb Verb { get; }
}
/// <summary>Consumes Health through typed damage resolution.</summary>
public sealed record DamageEffect(DamageType Type, int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Damage; }
/// <summary>Restores Health without resurrection or status removal.</summary>
public sealed record RestoreEffect(int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Restore; }
/// <summary>Adds delay to an existing future turn.</summary>
public sealed record DelayEffect(float Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Delay; }
/// <summary>Reduces qualifying damage per action and type; used as a persistent rule.</summary>
public sealed record ResistEffect(DamageFilter Filter, int Amount, bool ShareAcrossTypes = false) : ActionEffect { public override EffectVerb Verb => EffectVerb.Resist; }
/// <summary>Sets matching damage to zero; used as a persistent rule.</summary>
public sealed record NegateEffect(DamageFilter Filter) : ActionEffect { public override EffectVerb Verb => EffectVerb.Negate; }
/// <summary>Cancels incoming target effects; used as a persistent rule.</summary>
public sealed record EvadeEffect(ActionFilter Filter) : ActionEffect { public override EffectVerb Verb => EffectVerb.Evade; }
/// <summary>Moves a non-player into an unoccupied formation position.</summary>
public sealed record MoveEffect(FormationSlot Destination) : ActionEffect { public override EffectVerb Verb => EffectVerb.Move; }
/// <summary>Direction of a one-row formation shift.</summary>
public enum RowShiftDirection { Backward, Forward }
/// <summary>Moves each recipient one row, selecting the nearest free destination within that row.</summary>
public sealed record ShiftRowEffect(RowShiftDirection Direction) : ActionEffect { public override EffectVerb Verb => EffectVerb.Move; }
/// <summary>Excludes matching actions at selection and commitment; used as a persistent rule.</summary>
public sealed record RestrictEffect(ActionFilter Filter) : ActionEffect { public override EffectVerb Verb => EffectVerb.Restrict; }
/// <summary>Adds a signed adjustment to a supported numerical field; used as a persistent rule.</summary>
public sealed record ModifyEffect(EffectFilter Filter, NumericalField Field, float Adjustment) : ActionEffect { public override EffectVerb Verb => EffectVerb.Modify; }
/// <summary>Scales committed Turn costs while active; Slow and Haste share this persistent operation.</summary>
public sealed record TurnCostMultiplierEffect(float Multiplier) : ActionEffect { public override EffectVerb Verb => EffectVerb.Modify; }
/// <summary>Consumes up to the available quantity; unlike a cost this may partially apply.</summary>
public sealed record ConsumeEffect(ConsumableSubject Subject, int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Consume; }
/// <summary>Adds a resource, item, or action capability.</summary>
public sealed record GrantEffect(GrantSubject Subject) : ActionEffect { public override EffectVerb Verb => EffectVerb.Grant; }
/// <summary>Removes an entity or equipment association.</summary>
public sealed record RemoveEffect(RemoveSubject Subject) : ActionEffect { public override EffectVerb Verb => EffectVerb.Remove; }
/// <summary>Installs a defined persistent status and its runtime lifetime.</summary>
public sealed record ApplyEffect(StatusEffectId Status, StatusPersistence Persistence, bool ScaleDurationBySourceFocus = false) : ActionEffect { public override EffectVerb Verb => EffectVerb.Apply; }
/// <summary>Removes matching statuses, or shortens timed statuses by a whole number of turns; permanent lifetimes are unchanged by shortening.</summary>
public sealed record DispelEffect(StatusSelector Selector, int? Limit = null, int? DurationReduction = null) : ActionEffect { public override EffectVerb Verb => EffectVerb.Dispel; }
/// <summary>Atomically transfers a full quantity to an explicit destination character.</summary>
public sealed record TransferEffect(string DestinationId, ConsumableSubject Subject, int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Transfer; }
/// <summary>A full-availability commitment requirement, paid independently of target evasion.</summary>
public sealed record ActionCost(ConsumableSubject Subject, int Amount);
/// <summary>Equipment prerequisite for an action capability.</summary>
public sealed record EquipmentRequirement(string Slot, ItemId? Item = null);
/// <summary>Conjunctive progression, ownership, and equipment requirements.</summary>
public sealed record ActionRequirements
{
    /// <summary>Minimum current Focus required before commitment costs are paid.</summary>
    public int MinimumFocus { get; init; }
    /// <summary>All abilities that must have been learned.</summary>
    public ImmutableArray<AbilityId> Abilities { get; init; } = [];
    /// <summary>All item identities which must be owned.</summary>
    public ImmutableArray<ItemId> Items { get; init; } = [];
    /// <summary>All equipment associations required at commitment.</summary>
    public ImmutableArray<EquipmentRequirement> Equipment { get; init; } = [];
}
/// <summary>One effect's requested values, changes, prevention, and rule attribution.</summary>
public sealed record EffectOutcome(string EffectId, EffectVerb Verb, string RecipientId, ActionEffect Requested,
    double RequestedAmount, double ResolvedAmount, double ActualChange, EffectResult Result, string? Reason = null,
    ImmutableArray<string> Rules = default);
/// <summary>The complete committed action result, including its attributed source.</summary>
public sealed record ActionOutcome(ActionData Action, string SourceId, ImmutableArray<string> Recipients,
    ImmutableArray<EffectOutcome> Effects);
