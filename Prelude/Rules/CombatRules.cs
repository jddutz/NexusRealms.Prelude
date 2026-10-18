namespace NexusRealms.Prelude.Rules;

/// <summary>Rejects unsupported fields, ambiguous clocks, and malformed typed operations before commitment.</summary>
public static class CombatRules
{
    /// <summary>Validates a complete action definition and its ordered dependencies.</summary>
    public static void Validate(ActionData action, IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses)
    {
        if (string.IsNullOrWhiteSpace(action.Id) || !float.IsFinite(action.TurnCost) || action.TurnCost <= 0)
            throw new ArgumentException("Actions require an identity and positive finite Turn cost.");
        if (action.MeleeRange is { } reach && !Enum.IsDefined(reach)) throw new ArgumentException("Unknown melee reach.");
        if ((action.Flags & ~(ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee)) != 0)
            throw new ArgumentException("Unsupported action flags.");
        if (!Enum.IsDefined(action.Targeting.Shape) || !Enum.IsDefined(action.Targeting.Relationship)
            || (action.Targeting.Options & ~TargetingOptions.AllTeams) != 0) throw new ArgumentException("Unsupported targeting profile.");
        if (action.Requirements.MinimumFocus < 0) throw new ArgumentException("Minimum Focus must be nonnegative.");
        foreach (var equipment in action.Requirements.Equipment)
        {
            if (equipment.Slot is { } slot && !Combat.CombatLoadout.EquipmentSlots.Contains(slot))
                throw new ArgumentException("Unknown required equipment slot.");
            if (equipment.Item is { } item && string.IsNullOrWhiteSpace(item.Value))
                throw new ArgumentException("Required equipment item identity is empty.");
            if (!equipment.AnyOf.IsDefault && equipment.AnyOf.Any(kind => !Enum.IsDefined(kind)))
                throw new ArgumentException("Unknown required equipment kind.");
            if (equipment.Slot is null && equipment.Item is null && equipment.AnyOf.IsDefaultOrEmpty)
                throw new ArgumentException("Equipment requirements need a slot, item identity, or equipment kind.");
        }
        foreach (var cost in action.Costs) { Quantity(cost.Amount); Subject(cost.Subject); }
        var effects = action.BuildEffects();
        for (var index = 0; index < effects.Length; index++)
        {
            var effect = effects[index];
            if (effect.RequiresEffect is { } dependency && (dependency < 0 || dependency >= index)) throw new ArgumentException("Dependencies must name an earlier effect.");
            ValidateEffect(effect, false, statuses);
        }
    }

    /// <summary>Validates a status definition including required duplicate handling and persistent rules.</summary>
    public static void ValidateStatus(StatusEffectData definition, IReadOnlyDictionary<StatusEffectId, StatusEffectData>? statuses = null)
    {
        if (string.IsNullOrWhiteSpace(definition.Id.Value) || definition.Duplicates is not { } duplicates || !Enum.IsDefined(duplicates))
            throw new ArgumentException($"Status '{definition.Id}' must declare duplicate handling.");
        if (definition.Elevation < 0) throw new ArgumentException("Status elevation must be nonnegative.");
        if (!definition.FocusGainBonuses.IsEmpty && (definition.Persistence.Clock != StatusClock.Timeline
            || definition.FocusGainBonuses.Any(amount => amount < 0) || definition.FocusGainBonuses[^1] <= 0))
            throw new ArgumentException("Focus bonus sequences require a timed lifetime and a positive final bonus.");
        Persistence(definition.Persistence);
        definition.StrategyOverride?.Validate();
        foreach (var rule in definition.Rules) ValidateEffect(rule, true, new Dictionary<StatusEffectId, StatusEffectData>());
        var statusDefinitions = statuses ?? new Dictionary<StatusEffectId, StatusEffectData>();
        ValidateTriggeredEffects(definition.TurnStartEffects, statusDefinitions, "turn-start");
        ValidateDamageTrigger(definition.OnDamageDealt, statusDefinitions);
        ValidateDamageTrigger(definition.OnDamageTaken, statusDefinitions);
    }

    /// <summary>Validates one operation with strongly typed subjects and supported numerical fields.</summary>
    public static void ValidateEffect(ActionEffect effect, bool persistentRule, IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses)
    {
        if (!Enum.IsDefined(effect.Recipients)) throw new ArgumentException("Unknown recipient scope.");
        if (effect.AttackIndex < 0 || effect.MeleeRange is { } reach && !Enum.IsDefined(reach))
            throw new ArgumentException("Invalid attack index or melee reach.");
        var isRule = effect is ResistEffect or NegateEffect or RedirectDamageEffect or PreventEffect or PreventOperationEffect or EvadeEffect or RestrictEffect or ModifyEffect or DamageMultiplierEffect or TurnCostMultiplierEffect;
        if (isRule != persistentRule) throw new ArgumentException("Persistent rules belong to status or equipment definitions; operations belong to actions.");
        switch (effect)
        {
            case DamageEffect damage:
                Quantity(damage.Amount);
                if (!Enum.IsDefined(damage.Type)) throw new ArgumentException("Unknown damage type.");
                if (damage.RecipientTargeting is not null && damage.Recipients != RecipientScope.Targets)
                    throw new ArgumentException("Damage recipient targeting requires target-scoped damage.");
                if (damage.RecipientTargeting is { } recipientTargeting
                    && (!Enum.IsDefined(recipientTargeting.Shape) || !Enum.IsDefined(recipientTargeting.Relationship)
                        || (recipientTargeting.Options & ~TargetingOptions.AllTeams) != 0))
                    throw new ArgumentException("Unsupported damage recipient targeting profile.");
                break;
            case RestoreEffect restore: Quantity(restore.Amount); break;
            case DelayEffect delay: Number(delay.Amount); break;
            case ResistEffect resist: Quantity(resist.Amount); DamageFilter(resist.Filter); break;
            case NegateEffect negate: DamageFilter(negate.Filter); break;
            case RedirectDamageEffect redirect:
                DamageFilter(redirect.Filter);
                if (!Enum.IsDefined(redirect.Targeting.Shape) || !Enum.IsDefined(redirect.Targeting.Relationship)
                    || (redirect.Targeting.Options & ~TargetingOptions.AllTeams) != 0)
                    throw new ArgumentException("Unsupported damage redirection targeting profile.");
                break;
            case PreventEffect prevent: Identity(prevent.Status.Value); break;
            case PreventOperationEffect prevent: OperationFilter(prevent.Filter); break;
            case EvadeEffect evade: Filter(evade.Filter); break;
            case RestrictEffect restrict: Filter(restrict.Filter); break;
            case MoveEffect move: if (!Enum.IsDefined(move.Destination)) throw new ArgumentException("Unknown formation position."); break;
            case ShiftRowEffect shift: if (!Enum.IsDefined(shift.Direction)) throw new ArgumentException("Unknown row shift direction."); break;
            case ModifyEffect modify:
                if (!float.IsFinite(modify.Adjustment) || !Enum.IsDefined(modify.Field) || !Enum.IsDefined(modify.Filter.Verb)
                    || !Enum.IsDefined(modify.Filter.Recipient)
                    || modify.Filter.RequiredActionFlags is { } requiredFlags
                        && (requiredFlags & ~(ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee)) != 0)
                    throw new ArgumentException("Invalid modification.");
                var expected = modify.Field switch { NumericalField.DamageAmount => EffectVerb.Damage, NumericalField.RestoreAmount => EffectVerb.Restore,
                    NumericalField.DelayAmount => EffectVerb.Delay, NumericalField.StatusDuration => EffectVerb.Apply, _ => throw new ArgumentException("Unsupported numerical field.") };
                if (modify.Filter.Verb != expected || modify.Filter.DamageType is not null && expected != EffectVerb.Damage
                    || modify.Filter.DamageType is { } type && !Enum.IsDefined(type)
                    || modify.Field is NumericalField.DamageAmount or NumericalField.RestoreAmount or NumericalField.StatusDuration && modify.Adjustment != MathF.Truncate(modify.Adjustment))
                    throw new ArgumentException("Modification does not match a supported numerical field.");
                break;
            case DamageMultiplierEffect multiplier:
                if (!float.IsFinite(multiplier.Multiplier) || multiplier.Multiplier <= 0
                    || multiplier.Filter.Verb != EffectVerb.Damage
                    || !Enum.IsDefined(multiplier.Filter.Recipient)
                    || multiplier.Filter.DamageType is { } multiplierType && !Enum.IsDefined(multiplierType)
                    || multiplier.Filter.RequiredActionFlags is { } multiplierFlags
                        && (multiplierFlags & ~(ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee)) != 0)
                    throw new ArgumentException("Invalid damage multiplication.");
                break;
            case TurnCostMultiplierEffect scale:
                if (!float.IsFinite(scale.Multiplier) || scale.Multiplier <= 0)
                    throw new ArgumentException("Turn cost multipliers must be positive and finite.");
                break;
            case ConsumeEffect consume: Quantity(consume.Amount); Subject(consume.Subject); break;
            case GrantEffect grant:
                switch (grant.Subject)
                {
                    case GrantResource resource: Resource(resource.Resource); Quantity(resource.Amount); break;
                    case GrantItem item: if (item.Item.Quantity <= 0 || string.IsNullOrWhiteSpace(item.Item.Id.Value)) throw new ArgumentException("Invalid granted item."); break;
                    case GrantCapability capability: Identity(capability.ActionId); break;
                    default: throw new ArgumentException("Unsupported grant subject.");
                }
                break;
            case RemoveEffect remove:
                switch (remove.Subject)
                {
                    case RemoveItem item: Identity(item.Item.Value); break;
                    case RemoveEquipment equipment: if (!Combat.CombatLoadout.EquipmentSlots.Contains(equipment.Slot)) throw new ArgumentException("Unknown equipment slot."); break;
                    case DisarmEquipment: break;
                    case RemoveCapability capability: Identity(capability.ActionId); break;
                    default: throw new ArgumentException("Unsupported remove subject.");
                }
                break;
            case ApplyEffect apply:
                if (!statuses.ContainsKey(apply.Status)) throw new ArgumentException($"Unknown status '{apply.Status}'.");
                if (apply.ScaleDurationBySourceFocus && apply.Persistence.Clock == StatusClock.Permanent)
                    throw new ArgumentException("Focus-scaled durations require a timed status clock.");
                Persistence(apply.Persistence);
                if (apply.RuleOverrides is { } rules)
                    foreach (var rule in rules) ValidateEffect(rule, true, statuses);
                break;
            case DispelEffect dispel:
                if (dispel.Limit is { } limit) Quantity(limit);
                if (dispel.DurationReduction is { } reduction) Quantity(reduction);
                if (dispel.Selector.Id is { } id && !statuses.ContainsKey(id)) throw new ArgumentException($"Unknown status '{id}'.");
                break;
            case TransferEffect transfer: Identity(transfer.DestinationId); Quantity(transfer.Amount); Subject(transfer.Subject); break;
            default: throw new ArgumentException("Unsupported operation class.");
        }
    }

    /// <summary>Validates explicit persistence clocks, boundaries, and use consumption.</summary>
    public static void Persistence(StatusPersistence persistence)
    {
        if (!Enum.IsDefined(persistence.Clock) || !Enum.IsDefined(persistence.UseEvent)) throw new ArgumentException("Unsupported status lifetime.");
        Number(persistence.Duration);
        if (persistence.Clock == StatusClock.Permanent ? persistence.Duration != 0 : persistence.Duration <= 0)
            throw new ArgumentException("Permanent statuses have no duration; timed statuses require a positive duration.");
        if (persistence.Clock == StatusClock.RecipientTurnStart && (persistence.Duration != MathF.Truncate(persistence.Duration) || persistence.Duration > (double)int.MaxValue))
            throw new ArgumentException("Recipient turn durations require a whole count.");
        if (persistence.Uses is { } uses && (uses <= 0 || persistence.UseEvent == StatusUseEvent.Never)
            || persistence.Uses is null && persistence.UseEvent != StatusUseEvent.Never) throw new ArgumentException("Bounded uses require an explicit qualifying event.");
        if (persistence.UseFilter is { } filter) Filter(filter);
        if (persistence.UseDamageFilter is { } damageFilter)
        {
            if (persistence.Uses is null) throw new ArgumentException("Damage use filters require bounded status uses.");
            DamageFilter(damageFilter);
        }
    }
    /// <summary>Validates a non-Health quantity reference.</summary>
    private static void Subject(ConsumableSubject subject)
    {
        switch (subject) { case ResourceSubject resource: Resource(resource.Resource); break;
            case ItemQuantitySubject item: Identity(item.Item.Value); break; default: throw new ArgumentException("Unsupported quantity subject."); }
    }
    /// <summary>Validates an action filter combination rule.</summary>
    private static void Filter(ActionFilter filter) { const ActionFlags supportedFlags = ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee;
        if (!Enum.IsDefined(filter.Match) || filter.SpendsResource is { } resource && !Enum.IsDefined(resource)
            || filter.RequiresActionFlags is { } flags && (flags & ~supportedFlags) != 0
            || filter.MissingActionFlags is { } missingFlags && (missingFlags & ~supportedFlags) != 0)
            throw new ArgumentException("Unsupported action filter."); }
    /// <summary>Validates a typed damage filter.</summary>
    private static void DamageFilter(DamageFilter filter)
    {
        if (!Enum.IsDefined(filter.Match) || filter.Type is { } type && !Enum.IsDefined(type)
            || filter.Category is { } category && !Enum.IsDefined(category)
            || filter.RequiresActionFlags is { } flags
                && (flags & ~(ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee)) != 0)
            throw new ArgumentException("Invalid damage filter.");
    }
    /// <summary>Validates status-triggered damage operations and their ordered dependencies.</summary>
    private static void ValidateDamageTrigger(DamageStatusTrigger? trigger, IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses)
    {
        if (trigger is null) return;
        DamageFilter(trigger.Filter);
        ValidateTriggeredEffects(trigger.Effects, statuses, "damage-trigger");
    }
    /// <summary>Validates an ordered sequence of non-persistent status operations.</summary>
    private static void ValidateTriggeredEffects(
        ImmutableArray<ActionEffect> effects,
        IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses,
        string sequenceName
    )
    {
        for (var index = 0; index < effects.Length; index++)
        {
            var effect = effects[index];
            if (effect.RequiresEffect is { } dependency && (dependency < 0 || dependency >= index))
                throw new ArgumentException($"Dependencies must name an earlier {sequenceName} effect.");
            ValidateEffect(effect, false, statuses);
        }
    }
    /// <summary>Validates the operation, damage type, action flags, and rule-owner criteria.</summary>
    /// <param name="filter">The operation filter to validate.</param>
    private static void OperationFilter(EffectFilter filter)
    {
        const ActionFlags supportedFlags = ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee;
        if (!Enum.IsDefined(filter.Verb)
            || filter.DamageType is { } type && !Enum.IsDefined(type)
            || !Enum.IsDefined(filter.Recipient)
            || filter.RequiredActionFlags is { } flags && (flags & ~supportedFlags) != 0)
            throw new ArgumentException("Invalid operation filter.");
    }
    /// <summary>Validates declared resource kinds.</summary>
    private static void Resource(ResourceKind resource) { if (!Enum.IsDefined(resource)) throw new ArgumentException("Unsupported resource."); }
    /// <summary>Rejects empty subject identities.</summary>
    private static void Identity(string id) { if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Identity is required."); }
    /// <summary>Rejects negative base quantities.</summary>
    private static void Quantity(int amount) => ArgumentOutOfRangeException.ThrowIfNegative(amount);
    /// <summary>Rejects negative or nonfinite base amounts.</summary>
    private static void Number(float amount) { if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); }
}
