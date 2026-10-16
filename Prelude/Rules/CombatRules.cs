namespace NexusRealms.Prelude.Rules;

/// <summary>Rejects unsupported fields, ambiguous clocks, and malformed typed operations before commitment.</summary>
public static class CombatRules
{
    /// <summary>Validates a complete action definition and its ordered dependencies.</summary>
    public static void Validate(ActionData action, IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses)
    {
        if (string.IsNullOrWhiteSpace(action.Id) || !float.IsFinite(action.TurnCost) || action.TurnCost <= 0)
            throw new ArgumentException("Actions require an identity and positive finite Turn cost.");
        if (!Enum.IsDefined(action.Targeting.Shape) || !Enum.IsDefined(action.Targeting.Relationship)) throw new ArgumentException("Unsupported targeting profile.");
        foreach (var equipment in action.Requirements.Equipment)
            if (!Combat.CombatLoadout.EquipmentSlots.Contains(equipment.Slot)) throw new ArgumentException("Unknown required equipment slot.");
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
    public static void ValidateStatus(StatusEffectData definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id.Value) || definition.Duplicates is not { } duplicates || !Enum.IsDefined(duplicates))
            throw new ArgumentException($"Status '{definition.Id}' must declare duplicate handling.");
        Persistence(definition.Persistence);
        definition.StrategyOverride?.Validate();
        foreach (var rule in definition.Rules) ValidateEffect(rule, true, new Dictionary<StatusEffectId, StatusEffectData>());
    }

    /// <summary>Validates one operation with strongly typed subjects and supported numerical fields.</summary>
    public static void ValidateEffect(ActionEffect effect, bool persistentRule, IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses)
    {
        if (!Enum.IsDefined(effect.Recipients)) throw new ArgumentException("Unknown recipient scope.");
        var isRule = effect is ResistEffect or NegateEffect or EvadeEffect or RestrictEffect or ModifyEffect;
        if (isRule != persistentRule) throw new ArgumentException("Persistent rules belong to status or equipment definitions; operations belong to actions.");
        switch (effect)
        {
            case DamageEffect damage: Quantity(damage.Amount); if (!Enum.IsDefined(damage.Type)) throw new ArgumentException("Unknown damage type."); break;
            case RestoreEffect restore: Quantity(restore.Amount); break;
            case DelayEffect delay: Number(delay.Amount); break;
            case ResistEffect resist: Quantity(resist.Amount); DamageFilter(resist.Filter); break;
            case NegateEffect negate: DamageFilter(negate.Filter); break;
            case EvadeEffect evade: Filter(evade.Filter); break;
            case RestrictEffect restrict: Filter(restrict.Filter); break;
            case MoveEffect move: if (!Enum.IsDefined(move.Destination)) throw new ArgumentException("Unknown formation position."); break;
            case ModifyEffect modify:
                if (!float.IsFinite(modify.Adjustment) || !Enum.IsDefined(modify.Field) || !Enum.IsDefined(modify.Filter.Verb)) throw new ArgumentException("Invalid modification.");
                var expected = modify.Field switch { NumericalField.DamageAmount => EffectVerb.Damage, NumericalField.RestoreAmount => EffectVerb.Restore,
                    NumericalField.DelayAmount => EffectVerb.Delay, NumericalField.StatusDuration => EffectVerb.Apply, _ => throw new ArgumentException("Unsupported numerical field.") };
                if (modify.Filter.Verb != expected || modify.Filter.DamageType is not null && expected != EffectVerb.Damage
                    || modify.Filter.DamageType is { } type && !Enum.IsDefined(type)
                    || modify.Field is NumericalField.DamageAmount or NumericalField.RestoreAmount or NumericalField.StatusDuration && modify.Adjustment != MathF.Truncate(modify.Adjustment))
                    throw new ArgumentException("Modification does not match a supported numerical field.");
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
                    case RemoveCapability capability: Identity(capability.ActionId); break;
                    default: throw new ArgumentException("Unsupported remove subject.");
                }
                break;
            case ApplyEffect apply:
                if (!statuses.ContainsKey(apply.Status)) throw new ArgumentException($"Unknown status '{apply.Status}'.");
                Persistence(apply.Persistence); break;
            case DispelEffect dispel:
                if (dispel.Limit is { } limit) Quantity(limit);
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
    }
    /// <summary>Validates a non-Health quantity reference.</summary>
    private static void Subject(ConsumableSubject subject)
    {
        switch (subject) { case ResourceSubject resource: Resource(resource.Resource); break;
            case ItemQuantitySubject item: Identity(item.Item.Value); break; default: throw new ArgumentException("Unsupported quantity subject."); }
    }
    /// <summary>Validates an action filter combination rule.</summary>
    private static void Filter(ActionFilter filter) { if (!Enum.IsDefined(filter.Match)) throw new ArgumentException("Unsupported filter combination."); }
    /// <summary>Validates a typed damage filter.</summary>
    private static void DamageFilter(DamageFilter filter)
    {
        if (!Enum.IsDefined(filter.Match) || filter.Type is { } type && !Enum.IsDefined(type)) throw new ArgumentException("Invalid damage filter.");
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
