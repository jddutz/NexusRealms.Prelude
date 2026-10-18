using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>The sixteen operation names; targeting and costs are separate concerns.</summary>
public enum EffectVerb { Damage, Restore, Delay, Resist, Negate, Evade, Move, Restrict, Modify, Consume, Grant, Remove, Apply, Dispel, Transfer, Prevent }
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

/// <summary>Matches explicit action identity, classification, source, resource use, and action flags.</summary>
/// <param name="ActionId">Optional exact action identity.</param>
/// <param name="Classification">Optional action classification.</param>
/// <param name="SourceId">Optional exact action source identity.</param>
/// <param name="Match">How the declared criteria are combined.</param>
/// <param name="SpendsResource">Optional resource-spending criterion.</param>
/// <param name="RequiresActionFlags">Matches when all specified flags are present.</param>
/// <param name="MissingActionFlags">Matches when none of the specified flags are present.</param>
public sealed record ActionFilter(string? ActionId = null, string? Classification = null, string? SourceId = null, FilterMatch Match = FilterMatch.All, ResourceKind? SpendsResource = null, ActionFlags? RequiresActionFlags = null, ActionFlags? MissingActionFlags = null)
{
    /// <summary>Evaluates declared presence and absence criteria using the authored combination rule.</summary>
    public bool Matches(ActionData action, string source)
    {
        var tests = new List<bool>();
        if (ActionId is not null) tests.Add(action.Id == ActionId);
        if (Classification is not null) tests.Add(action.Classifications.Contains(Classification));
        if (SourceId is not null) tests.Add(source == SourceId);
        if (RequiresActionFlags is { } flags) tests.Add((action.Flags & flags) == flags);
        if (MissingActionFlags is { } missingFlags) tests.Add((action.Flags & missingFlags) == 0);
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
/// <summary>Source, damage-type, and optional action-affinity criteria for damage rules.</summary>
/// <param name="Type">Optional exact damage type.</param>
/// <param name="SourceId">Optional exact action source identity.</param>
/// <param name="Match">How the declared damage and source criteria are combined.</param>
/// <param name="Category">Optional damage category.</param>
/// <param name="RequiresActionFlags">Action affinities that must all be present.</param>
public sealed record DamageFilter(DamageType? Type = null, string? SourceId = null, FilterMatch Match = FilterMatch.All, DamageCategory? Category = null,
    ActionFlags? RequiresActionFlags = null)
{
    /// <summary>Tests a typed damage amount, its source, and optional action affinities.</summary>
    /// <param name="type">The damage type being resolved.</param>
    /// <param name="source">The action source identity.</param>
    /// <param name="actionFlags">The flags of the action dealing the damage.</param>
    /// <returns>True when every required affinity is present and the configured criteria match.</returns>
    public bool Matches(DamageType type, string source, ActionFlags actionFlags = ActionFlags.None)
    {
        if (RequiresActionFlags is { } requiredFlags && (actionFlags & requiredFlags) != requiredFlags)
            return false;
        var tests = new List<bool>();
        if (Type is { } expected) tests.Add(type == expected);
        if (SourceId is not null) tests.Add(source == SourceId);
        if (Category is { } category) tests.Add(type.GetCategory() == category);
        return tests.Count == 0 || (Match == FilterMatch.All ? tests.All(value => value) : tests.Any(value => value));
    }
}
/// <summary>Matches an operation by verb, optional damage type, action flags, and source or target owner.</summary>
/// <param name="Verb">The operation verb to match.</param>
/// <param name="DamageType">Optional damage type to match.</param>
/// <param name="RequiredActionFlags">Optional flags that must all be present on the action.</param>
/// <param name="Recipient">Whether the rule owner must be the operation's source or target.</param>
public sealed partial record EffectFilter(EffectVerb Verb, DamageType? DamageType = null,
    ActionFlags? RequiredActionFlags = null, RecipientScope Recipient = RecipientScope.Targets);
public sealed partial record EffectFilter
{
    /// <summary>Tests an operation against its verb, optional damage type and action flags, and rule-owner scope.</summary>
    /// <param name="effect">The operation being resolved.</param>
    /// <param name="actionFlags">The flags of the action executing the operation.</param>
    /// <param name="owner">Whether this rule belongs to the operation's source or target.</param>
    /// <returns>True when the operation matches every configured criterion.</returns>
    public bool Matches(ActionEffect effect, ActionFlags actionFlags, RecipientScope owner) =>
        Verb == effect.Verb
        && (DamageType is null || effect is DamageEffect damage && DamageType == damage.Type)
        && (RequiredActionFlags is not { } required || (actionFlags & required) == required)
        && Recipient == owner;
}
/// <summary>Explicit status persistence; duration units are determined by Clock.</summary>
/// <param name="Clock">The expiration clock for timed statuses.</param>
/// <param name="Duration">The explicit duration measured by Clock.</param>
/// <param name="Uses">Optional number of qualifying events before expiration.</param>
/// <param name="UseEvent">The incoming event that consumes a use.</param>
/// <param name="UseFilter">Optional action criteria required to consume a use.</param>
/// <param name="UseDamageFilter">Optional damage criteria required to consume a use.</param>
public sealed record StatusPersistence(StatusClock Clock = StatusClock.Permanent, float Duration = 0, int? Uses = null,
    StatusUseEvent UseEvent = StatusUseEvent.Never, ActionFilter? UseFilter = null, DamageFilter? UseDamageFilter = null);
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
/// <summary>Moves equipped weapons into the recipient's inventory and prevents re-equipping them until the scheduled return.</summary>
public sealed record DisarmEquipment : RemoveSubject;
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
public sealed record DamageEffect(DamageType Type, int Amount) : ActionEffect
{
    /// <summary>Optional recipient selection evaluated from each frozen action target.</summary>
    public TargetingProfile? RecipientTargeting { get; init; }

    /// <inheritdoc/>
    public override EffectVerb Verb => EffectVerb.Damage;
}
/// <summary>Restores Health without resurrection or status removal.</summary>
public sealed record RestoreEffect(int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Restore; }
/// <summary>Adds delay to an existing future turn.</summary>
public sealed record DelayEffect(float Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Delay; }
/// <summary>Reduces qualifying damage per action and type; used as a persistent rule.</summary>
public sealed record ResistEffect(DamageFilter Filter, int Amount, bool ShareAcrossTypes = false) : ActionEffect { public override EffectVerb Verb => EffectVerb.Resist; }
/// <summary>Sets matching damage to zero; used as a persistent rule.</summary>
public sealed record NegateEffect(DamageFilter Filter) : ActionEffect { public override EffectVerb Verb => EffectVerb.Negate; }
/// <summary>Routes matching damage from its selected recipient to eligible recipients; used as a persistent rule.</summary>
/// <param name="Filter">The damage type, category, source, and action-affinity criteria.</param>
/// <param name="Targeting">The eligible recipients selected relative to the original recipient.</param>
public sealed record RedirectDamageEffect(DamageFilter Filter, TargetingProfile Targeting) : ActionEffect
{
    /// <inheritdoc/>
    public override EffectVerb Verb => EffectVerb.Damage;
}
/// <summary>Blocks applications of the specified status; used as a persistent rule.</summary>
public sealed record PreventEffect(StatusEffectId Status) : ActionEffect { public override EffectVerb Verb => EffectVerb.Prevent; }
/// <summary>Blocks matching operations; used as a persistent rule.</summary>
public sealed record PreventOperationEffect(EffectFilter Filter) : ActionEffect { public override EffectVerb Verb => EffectVerb.Prevent; }
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
/// <summary>Multiplies matching damage by a positive factor; used as a persistent rule.</summary>
/// <param name="Filter">The damage, action, and rule-owner criteria.</param>
/// <param name="Multiplier">The positive damage factor.</param>
public sealed record DamageMultiplierEffect(EffectFilter Filter, float Multiplier) : ActionEffect { public override EffectVerb Verb => EffectVerb.Modify; }
/// <summary>Scales committed Turn costs while active; Slow and Haste share this persistent operation.</summary>
public sealed record TurnCostMultiplierEffect(float Multiplier) : ActionEffect { public override EffectVerb Verb => EffectVerb.Modify; }
/// <summary>Consumes up to the available quantity; unlike a cost this may partially apply.</summary>
public sealed record ConsumeEffect(ConsumableSubject Subject, int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Consume; }
/// <summary>Adds a resource, item, or action capability.</summary>
public sealed record GrantEffect(GrantSubject Subject) : ActionEffect { public override EffectVerb Verb => EffectVerb.Grant; }
/// <summary>Removes an entity or equipment association.</summary>
public sealed record RemoveEffect(RemoveSubject Subject) : ActionEffect { public override EffectVerb Verb => EffectVerb.Remove; }
/// <summary>Installs a defined persistent status and its runtime lifetime.</summary>
/// <summary>Applies a status with its authored behavior or instance-specific persistent rule overrides.</summary>
/// <param name="Status">The registered status definition to apply.</param>
/// <param name="Persistence">The lifetime and bounded-use policy for this application.</param>
/// <param name="ScaleDurationBySourceFocus">Whether a timed duration scales with the source's current Focus.</param>
public sealed record ApplyEffect(StatusEffectId Status, StatusPersistence Persistence, bool ScaleDurationBySourceFocus = false) : ActionEffect
{
    /// <summary>Optional persistent rules for this status instance, replacing the definition's rules.</summary>
    public ImmutableArray<ActionEffect>? RuleOverrides { get; init; }

    /// <inheritdoc/>
    public override EffectVerb Verb => EffectVerb.Apply;
}
/// <summary>Removes matching statuses, or shortens timed statuses by a whole number of turns; permanent lifetimes are unchanged by shortening.</summary>
public sealed record DispelEffect(StatusSelector Selector, int? Limit = null, int? DurationReduction = null) : ActionEffect { public override EffectVerb Verb => EffectVerb.Dispel; }
/// <summary>Atomically transfers a full quantity to an explicit destination character.</summary>
public sealed record TransferEffect(string DestinationId, ConsumableSubject Subject, int Amount) : ActionEffect { public override EffectVerb Verb => EffectVerb.Transfer; }
/// <summary>A full-availability commitment requirement, paid independently of target evasion.</summary>
public sealed record ActionCost(ConsumableSubject Subject, int Amount);
/// <summary>An equipped-item prerequisite; item categories match any listed kind.</summary>
/// <param name="Slot">Optional exact equipment slot.</param>
/// <param name="Item">Optional exact owned item identity.</param>
/// <param name="AnyOf">Optional equipment categories, of which the equipped item must match one.</param>
public sealed record EquipmentRequirement(string? Slot = null, ItemId? Item = null,
    ImmutableArray<EquipmentKind> AnyOf = default);
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
