using System.Collections.Immutable;
using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Deterministic action resolution over existing scheduler, health, and loadout state.</summary>
public sealed class CombatActionResolver
{
    private readonly Dictionary<string, ActionCharacterState> _characters = new(StringComparer.Ordinal);
    private readonly Dictionary<StatusEffectId, StatusEffectData> _statuses;
    private long _statusSequence;
    internal long NextApplicationSequence() => ++_statusSequence;
    internal long ApplicationSequence => _statusSequence;
    internal void RestoreApplicationSequence(long sequence) => _statusSequence = Math.Max(_statusSequence, sequence);
    private IReadOnlyDictionary<string, CharacterData> _definitions = new Dictionary<string, CharacterData>();
    /// <summary>Creates a resolver and validates every registered persistent definition.</summary>
    public CombatActionResolver(CombatSystem combat, IReadOnlyDictionary<StatusEffectId, StatusEffectData> statuses)
    {
        Combat = combat;
        combat.Resolver = this;
        _statuses = statuses.ToDictionary();
        foreach (var definition in _statuses.Values) ActionValidation.ValidateStatus(definition);
        combat.TurnStarted += OnTurnStarted;
        Behavior = new(this);
    }
    /// <summary>The shared encounter scheduler.</summary>
    public CombatSystem Combat { get; }
    public EncounterBehavior Behavior { get; }
    public DecisionTrace? LastDecision { get; internal set; }
    public bool CaptureCandidateTraces { get; set; } = true;
    public bool AllowEscape { get; init; }
    public void ConfigureCharacters(IReadOnlyDictionary<string, CharacterData> definitions)
    {
        _definitions = definitions.ToImmutableDictionary(StringComparer.Ordinal);
        foreach (var (id, definition) in definitions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            Behavior.Assign(id, BindAssignment(id, definition.Strategy));
        foreach (var (id, definition) in definitions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            foreach (var transition in definition.StrategyTransitions)
                Behavior.AddTransition(id, transition with { Destination = BindAssignment(id, transition.Destination),
                    Actors = transition.Actors.Select(reference => BindReference(id, reference)).ToImmutableArray(),
                    Condition = transition.Condition with { SubjectId = BindReference(id, transition.Condition.SubjectId) } });
        // Validate all capabilities, grants, and preview support before starting the encounter.
        foreach (var action in ActionCatalog.Values) ValidateContext(action);
        if (_characters.Values.Any(character => character.Loadout.Commands.Any(command => command.Resolve is not null)))
            throw new ArgumentException("AI cannot simulate opaque command callbacks; author typed effects instead.");
        _ = CopyForSimulation();
    }
    internal StrategyAssignment BindAssignment(string actorId, StrategyAssignment assignment) => assignment with
        { ProtectedIds = assignment.ProtectedIds?.Select(reference => BindReference(actorId, reference)).ToArray() };
    private string BindReference(string actorId, string reference)
    {
        if (reference == "$self") return actorId;
        if (_characters.ContainsKey(reference)) return reference;
        var matches = _definitions.Where(pair => pair.Value.Id.Value == reference).Select(pair => pair.Key).ToArray();
        return matches.Length == 1 ? matches[0] : throw new ArgumentException($"Character reference '{reference}' is unknown or ambiguous; use an explicit group selector for duplicates.");
    }
    /// <summary>Registered runtime characters; state is never copied into a second inventory.</summary>
    public IReadOnlyDictionary<string, ActionCharacterState> Characters => _characters;
    /// <summary>Resolved capabilities available to Grant and progression unlocks.</summary>
    public Dictionary<string, ActionData> ActionCatalog { get; } = new(StringComparer.Ordinal);
    /// <summary>Raised after each resolution for presentation, logging, and simulation.</summary>
    public event Action<ActionOutcome>? Resolved;
    /// <summary>Raised when authoritative formation state changes.</summary>
    public event Action<string, FormationSlot>? Moved;

    /// <summary>Registers existing mutable owners and expands authored initial status identifiers.</summary>
    public void Add(ActionCharacterState character, IEnumerable<StatusEffectId>? initialStatuses = null)
    {
        if (character.MaximumHealth is < 0 || character.Loadout.Focus < 0) throw new ArgumentException("Invalid initial character resources.");
        if (character.Position is { } position && _characters.Values.Any(other => other.Team == character.Team && other.Position == position))
            throw new ArgumentException("Formation position is occupied.");
        foreach (var rule in character.Rules) ActionValidation.ValidateEffect(rule, true, _statuses);
        var initial = (initialStatuses ?? []).ToArray();
        foreach (var id in initial) if (!_statuses.ContainsKey(id)) throw new ArgumentException($"Unknown initial status '{id}'.");
        _characters.Add(character.Id, character);
        character.Loadout.CommandAllowed = command => Allows(character.Id, character.Loadout.Actions.GetValueOrDefault(command.Id.Value) ?? new ActionData
        {
            Id = command.Id.Value,
            Classifications = command.Hand is not null || command == CombatLoadout.Throw ? ["weapon"] : command.Classifications.IsDefault ? [] : command.Classifications,
        });
        foreach (var action in character.Loadout.Actions.Values) { Register(action); character.Loadout.AddActionCommand(action); }
        foreach (var id in initial) ApplyStatus(character, id, character.Id, _statuses[id].Persistence);
    }

    /// <summary>Validates and registers an authored action class.</summary>
    public void Register(ActionData action)
    {
        ActionValidation.Validate(action, _statuses);
        ActionCatalog[action.Id] = action;
    }

    /// <summary>Registers progression-gated definitions without learning them or granting their capabilities.</summary>
    public void RegisterAbility(AbilityData ability)
    {
        foreach (var action in ability.Actions)
            Register(action with { Requirements = action.Requirements with { Abilities = action.Requirements.Abilities.Add(ability.Id) } });
    }

    /// <summary>Checks live restrictions for legacy playback without paying or resolving an action.</summary>
    public bool Allows(string sourceId, ActionData action)
    {
        Expire();
        return _characters.TryGetValue(sourceId, out var source) && source.Health.Health > 0
            && source.Loadout.MeetsRequirements(action) && !Restricted(source, action)
            && action.Costs.GroupBy(cost => cost.Subject).All(group => Available(source, group.Key) >= group.Sum(cost => (long)cost.Amount));
    }

    /// <summary>Routes a committed legacy strike through the same typed effects and persistent rules.</summary>
    public ActionOutcome ResolveDamage(string sourceId, string targetId, string actionId, DamageProfile damage, AttackImpact? impact = null)
        => ResolveCommitted(new ActionData { Id = actionId, Classifications = ["weapon"], Damage = damage }, sourceId, [targetId], impact);

    /// <summary>Checks current capability, restrictions, equipment, full costs, and targeting.</summary>
    public bool CanCommit(string sourceId, ActionData action, string? targetId)
    {
        ValidateContext(action);
        Expire();
        if (!_characters.TryGetValue(sourceId, out var source) || source.Health.Health == 0 || Combat.HasEnded
            || Combat.IsResolving || Combat.ActiveCombatant?.Id != sourceId || !source.Loadout.Actions.TryGetValue(action.Id, out var capability) || capability != action
            || !source.Loadout.MeetsRequirements(action) || Restricted(source, action)) return false;
        if (action.Costs.GroupBy(cost => cost.Subject).Any(group => Available(source, group.Key) < group.Sum(cost => (long)cost.Amount))) return false;
        if (action.Effects is null && action.ItemId is not null)
        {
            var prepared = new EnemyActionState(source.Loadout).Prepare(action);
            if (prepared is null) return false;
            if (prepared.Reload) return true;
            var hand = Enum.GetValues<StrikeHand>().FirstOrDefault(hand => source.Loadout.HeldItem(hand)?.Id == prepared.ItemId);
            if (!prepared.Throw && !source.Loadout.ValidTarget(hand == StrikeHand.Left ? CombatLoadout.LeftHandStrike : CombatLoadout.RightHandStrike,
                CandidateGeneration.Target(this, sourceId, targetId))) return false;
        }
        var recipients = action.Targeting.Select(this, sourceId, targetId);
        if (recipients.Length == 0) return false;
        if (action.BuildEffects().OfType<MoveEffect>().Any(effect =>
            (effect.Recipients == RecipientScope.Source ? new[] { sourceId } : recipients.ToArray()).Any(id => _characters[id].IsPlayer))) return false;
        return true;
    }

    /// <summary>Commits a class-based action atomically; invalid actions pay no costs.</summary>
    public ActionOutcome Commit(string sourceId, ActionData action, string? targetId)
    {
        if (action.Effects is null && action.ItemId is not null)
        {
            var prepared = new EnemyActionState(_characters[sourceId].Loadout).Prepare(action)
                ?? throw new InvalidOperationException("Owned legacy weapon or ammunition unavailable.");
            return Commit(sourceId, new ActionCandidate(prepared, targetId) { Kind = CandidateKind.LegacyWeapon, ItemId = action.ItemId.Value.Value });
        }
        if (!CanCommit(sourceId, action, targetId)) throw new InvalidOperationException("Action is unavailable for the current state.");
        var source = _characters[sourceId];
        ActionOutcome? outcome = null;
        var operation = new CombatAction(action.TurnCost, (_, _) =>
        {
            // Costs were aggregated and validated before any mutation; evasion never refunds them.
            foreach (var cost in action.Costs) Consume(source, cost.Subject, cost.Amount);
            var recipients = action.Targeting.Select(this, sourceId, targetId);
            outcome = ResolveCommitted(action, sourceId, recipients);
        });
        if (Combat.ActiveCombatant!.PlayerControlled) Combat.SubmitAction(operation);
        else Combat.SubmitAutomaticAction(operation);
        return outcome ?? throw new InvalidOperationException("Committed action did not resolve.");
    }

    /// <summary>Clones all mutable combat owners and rebinds simulation-safe scheduled effects.</summary>
    public CombatActionResolver CopyForSimulation()
    {
        var copy = new CombatActionResolver(Combat.CopyForPreview(), _statuses) { _statusSequence = _statusSequence,
            AllowEscape = AllowEscape, CaptureCandidateTraces = CaptureCandidateTraces, _definitions = _definitions };
        foreach (var original in _characters.Values)
        {
            var character = new ActionCharacterState(original.Id, original.IsPlayer, original.Team,
                new DamageReceiver(original.Health.Health), original.Loadout.CopyForPreview(), original.Position)
            { MaximumHealth = original.MaximumHealth, Rules = original.Rules.ToArray(), TurnsStarted = original.TurnsStarted };
            copy.Add(character);
            character.ActiveStatuses.AddRange(original.ActiveStatuses.Select(status => status.CopyForPreview()));
            character.Health.DamageTaken += result => { if (result.IsLethal) copy.Combat.Remove(character.Id); };
        }
        copy.Combat.IsCombatantAlive = id => copy._characters[id].Health.Health > 0;
        copy.ActionCatalog.Clear();
        foreach (var pair in ActionCatalog) copy.ActionCatalog.Add(pair.Key, pair.Value);
        Behavior.CopyTo(copy.Behavior);
        return copy;
    }

    /// <summary>Executes ordinary commitment, including scheduling and terminal objectives, without pumping future occurrences.</summary>
    public ActionPreview Preview(string sourceId, ActionData action, string? targetId) => Preview(sourceId, new ActionCandidate(action, targetId));
    public ActionPreview Preview(string sourceId, ActionCandidate candidate)
    {
        var copy = CopyForSimulation();
        var outcome = copy.Commit(sourceId, candidate);
        return ActionPreview.Capture(outcome, copy);
    }

    /// <summary>Schedules authored effects with mutable state resolved from the supplied simulation.</summary>
    public long ScheduleEvent(EncounterEventData occurrence)
    {
        if (!occurrence.KnownToAi) throw new ArgumentException("Unrevealed encounter events require an observation-based simulation view; fully visible AI cannot preview them.");
        ValidateContext(occurrence.Action);
        if (!_characters.ContainsKey(occurrence.SourceId) || !_characters.ContainsKey(occurrence.RecipientId))
            throw new ArgumentException("Unknown authored event source or recipient.");
        return Combat.ScheduleSimulationEvent(occurrence.Turn, occurrence.Label,
            system => system.Resolver!.ResolveCommitted(occurrence.Action, occurrence.SourceId, [occurrence.RecipientId]),
            occurrence.Priority, occurrence.Icon, occurrence.IsVisible, occurrence.PresentationKey);
    }

    public bool CanCommit(string sourceId, ActionCandidate candidate)
    {
        if (!_characters.TryGetValue(sourceId, out var source) || Combat.HasEnded || Combat.IsResolving
            || Combat.ActiveCombatant?.Id != sourceId || !Combat.IsAlive(sourceId)) return false;
        switch (candidate.Kind)
        {
            case CandidateKind.Escape: return source.IsPlayer && AllowEscape && !Combat.IsEscaping;
            case CandidateKind.Capability: return CanCommit(sourceId, candidate.Action, candidate.TargetId);
            case CandidateKind.LegacyWeapon:
                if (!source.Loadout.Actions.TryGetValue(candidate.Action.Id, out var original)) return false;
                var prepared = new EnemyActionState(source.Loadout).Prepare(original);
                if (prepared is null || prepared.ItemId?.Value != candidate.ItemId || prepared.Reload != candidate.Action.Reload
                    || prepared.TurnCost != candidate.Action.TurnCost || !prepared.BuildEffects().SequenceEqual(candidate.Action.BuildEffects())
                    || !Allows(sourceId, original)) return false;
                return CanCommit(sourceId, original, candidate.TargetId);
            case CandidateKind.ItemLayout:
                return source.Loadout.CanCommitItemLayout(Combat, sourceId, candidate.Layout, candidate.Reloads);
            case CandidateKind.Command:
                var command = source.Loadout.Find(candidate.Action.Id);
                return command is not null && command.Resolve is null && source.Loadout.CanConfirm(Combat, sourceId, command,
                    source.Loadout.ValidTarget(command, CandidateGeneration.Target(this, sourceId, candidate.TargetId)))
                    && (command.Hand is not { } hand || source.Loadout.HeldItem(hand)?.Id.Value == candidate.ItemId);
            case CandidateKind.Throw:
                return source.Loadout.ThrowOptions().Any(option => option.Item.Id.Value == candidate.ItemId)
                    && source.Loadout.CanConfirm(Combat, sourceId, CombatLoadout.Throw,
                        source.Loadout.ValidTarget(CombatLoadout.Throw, CandidateGeneration.Target(this, sourceId, candidate.TargetId)));
            default: throw new ArgumentException("Unknown candidate kind.");
        }
    }

    internal string ExplainUnavailable(string sourceId, ActionCandidate candidate)
    {
        if (Combat.HasEnded) return "Encounter has ended.";
        if (Combat.ActiveCombatant?.Id != sourceId || Combat.IsResolving) return "Actor has no pending decision boundary.";
        var source = _characters[sourceId];
        if (!Combat.IsAlive(sourceId)) return "Actor is incapacitated or unscheduled.";
        if (Restricted(source, candidate.Action)) return "An active restriction matches this action.";
        if (!source.Loadout.MeetsRequirements(candidate.Action)) return "Progression, ownership or equipment prerequisites are unmet.";
        foreach (var group in candidate.Action.Costs.GroupBy(cost => cost.Subject))
            if (Available(source, group.Key) < group.Sum(cost => (long)cost.Amount)) return $"Insufficient quantity for {group.Key}.";
        if (candidate.Kind == CandidateKind.ItemLayout) return "Inventory layout, reload ammunition, capacity or equipment rules reject this transaction.";
        if (candidate.Kind == CandidateKind.Throw) return "Selected item cannot be thrown with the current hands, ownership or target.";
        if (candidate.Kind == CandidateKind.Command && !source.Loadout.BasicCommands) return "Basic command is not part of this character's repertoire.";
        if (candidate.TargetId is { } target && !Combat.IsAlive(target)) return "Selected recipient or anchor is incapacitated or unscheduled.";
        return "Selected recipient, anchor, range, hand or capability fails ordinary commitment validation.";
    }

    public ActionOutcome Commit(string sourceId, ActionCandidate candidate)
    {
        if (!CanCommit(sourceId, candidate)) throw new InvalidOperationException("Stale or invalid AI candidate; no costs paid.");
        if (candidate.Kind == CandidateKind.Capability) return Commit(sourceId, candidate.Action, candidate.TargetId);
        var source = _characters[sourceId];
        ActionOutcome? outcome = null;
        void EmptyOutcome() { outcome = new(candidate.Action, sourceId, [], []); Resolved?.Invoke(outcome); }
        switch (candidate.Kind)
        {
            case CandidateKind.Escape:
                if (!Combat.RequestEscape(sourceId)) throw new InvalidOperationException("Escape invalidated before commitment.");
                outcome = new(candidate.Action, sourceId, [], []);
                break;
            case CandidateKind.LegacyWeapon:
                var original = source.Loadout.Actions[candidate.Action.Id];
                var prepared = new EnemyActionState(source.Loadout).Prepare(original)!;
                var operation = new CombatAction(prepared.TurnCost, (_, _) =>
                {
                    foreach (var cost in original.Costs) Consume(source, cost.Subject, cost.Amount);
                    new EnemyActionState(source.Loadout).Apply(prepared);
                    outcome = ResolveCommitted(prepared, sourceId, prepared.Reload ? [sourceId] : original.Targeting.Select(this, sourceId, candidate.TargetId));
                });
                if (Combat.ActiveCombatant!.PlayerControlled) Combat.SubmitAction(operation); else Combat.SubmitAutomaticAction(operation);
                break;
            case CandidateKind.Command:
                var command = source.Loadout.Find(candidate.Action.Id)!;
                if (!source.Loadout.Confirm(Combat, sourceId, command,
                    () => CandidateGeneration.Target(this, sourceId, candidate.TargetId),
                    () => { if (outcome is null) EmptyOutcome(); },
                    strike => outcome = ResolveDamage(sourceId, candidate.TargetId!, command.Id.Value, strike.DamagePoints, AttackImpact.ForStrike(strike))))
                    throw new InvalidOperationException("Command invalidated before commitment.");
                break;
            case CandidateKind.Throw:
                var thrownItem = source.Loadout.Items.Single(item => item.Id.Value == candidate.ItemId);
                source.Loadout.PrepareThrow(candidate.ItemId!);
                if (!source.Loadout.ConfirmPreparedThrow(Combat, sourceId, () => CandidateGeneration.Target(this, sourceId, candidate.TargetId),
                    damage => outcome = ResolveDamage(sourceId, candidate.TargetId!, "Throw", damage, AttackImpact.ForThrow(thrownItem))))
                    throw new InvalidOperationException("Throw invalidated before commitment.");
                break;
            case CandidateKind.ItemLayout:
                // Equipment commitment has no effects; its inventory/resource changes remain in the resulting state.
                if (!source.Loadout.CommitItemLayout(Combat, sourceId, candidate.Layout.ToDictionary(), candidate.Reloads))
                    throw new InvalidOperationException("Item layout invalidated before commitment.");
                outcome = new(candidate.Action, sourceId, [], []);
                break;
        }
        return outcome ?? throw new InvalidOperationException("Candidate did not resolve.");
    }

    internal ActionOutcome RecordEmptyAction(string sourceId, string actionId, float turnCost)
    {
        var outcome = new ActionOutcome(new ActionData { Id = actionId, TurnCost = turnCost, Damage = new DamageProfile() }, sourceId, [], []);
        Resolved?.Invoke(outcome);
        return outcome;
    }

    /// <summary>Resolves effects of an already committed action, adapting existing attack playback without charging its costs again.</summary>
    public ActionOutcome ResolveCommitted(ActionData action, string sourceId, IEnumerable<string> recipients, AttackImpact? impact = null)
    {
        ValidateContext(action);
        if (!_characters.ContainsKey(sourceId)) throw new ArgumentException("Unknown action source.");
        var targets = recipients.Distinct(StringComparer.Ordinal).ToImmutableArray();
        if (targets.Any(id => !_characters.ContainsKey(id))) throw new ArgumentException("Unknown action recipient.");
        Expire();
        var activeAtStart = targets.ToDictionary(id => id, id => _characters[id].ActiveStatuses.ToArray());
        var evasions = targets.ToDictionary(id => id, id => Rules(_characters[id]).Where(rule => rule.Effect is EvadeEffect evade && evade.Filter.Matches(action, sourceId)).Select(rule => rule.Id).ToImmutableArray());
        var results = ImmutableArray.CreateBuilder<EffectOutcome>();
        var changed = new Dictionary<(int Index, string Recipient), bool>();
        var spentResistance = new Dictionary<(string Recipient, string Rule, DamageType Type), int>();
        var effects = action.BuildEffects();
        for (var index = 0; index < effects.Length; index++)
        {
            var effect = effects[index];
            var selected = effect.Recipients == RecipientScope.Source ? ImmutableArray.Create(sourceId) : targets;
            foreach (var id in selected)
            {
                var recipient = _characters[id];
                var effectId = effect.Id ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                EffectOutcome result;
                if (effect.Recipients == RecipientScope.Targets && evasions[id].Length > 0)
                    result = new(effectId, effect.Verb, id, effect, Requested(effect), 0, 0, EffectResult.Evaded, "Incoming action evaded for this recipient.", evasions[id]);
                else if (effect.RequiresEffect is { } dependency && !changed.GetValueOrDefault((dependency, id)))
                    result = new(effectId, effect.Verb, id, effect, Requested(effect), 0, 0, EffectResult.Unchanged, "Required earlier effect did not change this recipient.");
                else result = ResolveEffect(effectId, effect, recipient, sourceId, spentResistance, impact);
                changed[(index, id)] = result.ActualChange != 0;
                results.Add(result);
            }
        }
        foreach (var id in targets)
            foreach (var status in activeAtStart[id])
            {
                if (status.RemainingUses is null || !(_characters[id].ActiveStatuses.Contains(status))
                    || status.Persistence.UseFilter is { } filter && !filter.Matches(action, sourceId)) continue;
                if (status.Persistence.UseEvent == StatusUseEvent.IncomingAction
                    || status.Persistence.UseEvent == StatusUseEvent.NonEvadedIncomingAction && evasions[id].Length == 0)
                {
                    status.RemainingUses--;
                    if (status.RemainingUses == 0) _characters[id].ActiveStatuses.Remove(status);
                }
            }
        var outcome = new ActionOutcome(action, sourceId, targets, results.ToImmutable());
        Resolved?.Invoke(outcome);
        return outcome;
    }

    /// <summary>Resolves an operation against current state; independent failures yield explicit no-change results.</summary>
    private EffectOutcome ResolveEffect(string effectId, ActionEffect effect, ActionCharacterState recipient, string source,
        Dictionary<(string Recipient, string Rule, DamageType Type), int> spentResistance, AttackImpact? impact)
    {
        var requested = Requested(effect);
        double resolved = requested, actual = 0;
        var result = EffectResult.Applied;
        string? reason = null;
        var attribution = ImmutableArray.CreateBuilder<string>();
        switch (effect)
        {
            case DamageEffect damage:
            {
                var amount = (int)Modified(recipient, damage, NumericalField.DamageAmount, damage.Amount, attribution);
                var rules = Rules(recipient).ToArray();
                var negations = rules.Where(rule => rule.Effect is NegateEffect negate && negate.Filter.Matches(damage.Type, source)).ToArray();
                if (negations.Length > 0)
                {
                    amount = 0; result = EffectResult.Negated; attribution.AddRange(negations.Select(rule => rule.Id));
                }
                else foreach (var rule in rules)
                {
                    if (rule.Effect is not ResistEffect resist || !resist.Filter.Matches(damage.Type, source)) continue;
                    var key = (recipient.Id, rule.Id, damage.Type);
                    var reduction = Math.Min(amount, Math.Max(0, resist.Amount - spentResistance.GetValueOrDefault(key)));
                    if (reduction == 0) continue;
                    spentResistance[key] = spentResistance.GetValueOrDefault(key) + reduction;
                    amount -= reduction; attribution.Add(rule.Id); result = EffectResult.Reduced;
                }
                resolved = amount;
                actual = DamageResolver.Apply(recipient.Health, new(new Dictionary<DamageType, int> { [damage.Type] = amount }), impact: impact).HealthLost;
                break;
            }
            case RestoreEffect restore:
            {
                resolved = Modified(recipient, restore, NumericalField.RestoreAmount, restore.Amount, attribution);
                // Restore does not define resurrection. Dead characters remain dead and unscheduled.
                if (recipient.Health.Health == 0) { reason = "Restore does not resurrect an incapacitated character."; break; }
                var after = (int)Math.Min(recipient.MaximumHealth ?? int.MaxValue, recipient.Health.Health + resolved);
                after = Math.Max(after, recipient.Health.Health);
                actual = after - recipient.Health.Health; recipient.Health.Health = after;
                break;
            }
            case DelayEffect delay:
            {
                resolved = Modified(recipient, delay, NumericalField.DelayAmount, delay.Amount, attribution);
                var before = Combat.Timeline.TurnOrder.FirstOrDefault(turn => turn.CombatantId == recipient.Id)?.Turn;
                if (Combat.TryDelay(recipient.Id, (float)resolved))
                    actual = Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == recipient.Id).Turn - before!.Value;
                else reason = "Recipient has no future Turn or delay cannot advance it.";
                break;
            }
            case MoveEffect move:
                if (recipient.IsPlayer) reason = "Player has no formation position.";
                else if (recipient.Position == move.Destination) reason = "Recipient already occupies the destination.";
                else if (_characters.Values.Any(other => other.Id != recipient.Id && other.Team == recipient.Team && other.Position == move.Destination && other.Health.Health > 0)) reason = "Destination is occupied.";
                else
                {
                    recipient.Position = move.Destination;
                    if (Combat.Contains(recipient.Id)) Combat.Relocate(recipient.Id, Row(move.Destination));
                    Moved?.Invoke(recipient.Id, move.Destination); actual = 1;
                }
                break;
            case ConsumeEffect consume: actual = Consume(recipient, consume.Subject, consume.Amount); break;
            case GrantEffect grant:
                switch (grant.Subject)
                {
                    case GrantResource resource:
                        actual = Math.Min(resource.Amount, int.MaxValue - recipient.Loadout.Focus);
                        recipient.Loadout.Focus += (int)actual; break;
                    case GrantItem item: actual = recipient.Loadout.TryAddItem(item.Item) ? item.Item.Quantity : 0; break;
                    case GrantCapability capability:
                        actual = recipient.Loadout.Actions.TryAdd(capability.ActionId, ActionCatalog[capability.ActionId]) ? 1 : 0;
                        if (actual > 0) recipient.Loadout.AddActionCommand(ActionCatalog[capability.ActionId]);
                        break;
                }
                break;
            case RemoveEffect remove:
                actual = remove.Subject switch
                {
                    RemoveItem item => recipient.Loadout.RemoveOwnedItem(item.Item) ? 1 : 0,
                    RemoveEquipment equipment => recipient.Loadout.Unequip(equipment.Slot) ? 1 : 0,
                    RemoveCapability capability => RemoveCapability(recipient, capability.ActionId),
                    _ => 0,
                };
                break;
            case ApplyEffect apply:
                var persistence = apply.Persistence;
                if (persistence.Clock != StatusClock.Permanent)
                {
                    resolved = Modified(recipient, apply, NumericalField.StatusDuration, persistence.Duration, attribution);
                    if (persistence.Clock == StatusClock.RecipientTurnStart) resolved = Math.Floor(resolved);
                    if (resolved <= 0) { reason = "Modified status duration is zero."; break; }
                    persistence = persistence with { Duration = (float)resolved };
                }
                actual = ApplyStatus(recipient, apply.Status, source, persistence) ? 1 : 0;
                break;
            case DispelEffect dispel:
                var matches = recipient.ActiveStatuses.Where(status => (dispel.Selector.Id is null || status.Definition.Id == dispel.Selector.Id)
                    && (dispel.Selector.Tag is null || status.Definition.Tags.Contains(dispel.Selector.Tag)))
                    .OrderBy(status => status.Sequence).Take(dispel.Limit ?? int.MaxValue).ToArray();
                foreach (var status in matches) recipient.ActiveStatuses.Remove(status);
                actual = matches.Length;
                break;
            case TransferEffect transfer:
                if (!_characters.TryGetValue(transfer.DestinationId, out var destination) || destination == recipient || transfer.Amount == 0)
                    reason = "Invalid transfer destination or quantity.";
                else if (Available(recipient, transfer.Subject) < transfer.Amount) reason = "Full transfer quantity is unavailable.";
                else if (transfer.Subject is ResourceSubject)
                {
                    if (destination.Loadout.Focus > int.MaxValue - transfer.Amount) reason = "Destination resource capacity exceeded.";
                    else { recipient.Loadout.Focus -= transfer.Amount; destination.Loadout.Focus += transfer.Amount; actual = transfer.Amount; }
                }
                else if (transfer.Subject is ItemQuantitySubject item)
                {
                    actual = recipient.Loadout.TransferItemTo(destination.Loadout, item.Item, transfer.Amount) ? transfer.Amount : 0;
                    if (actual == 0) reason = "Item transfer requires a whole stack, unique identity, and free destination inventory space.";
                }
                break;
        }
        if (actual == 0 && result == EffectResult.Applied) { result = EffectResult.Unchanged; reason ??= "Operation made no change to current state."; }
        return new(effectId, effect.Verb, recipient.Id, effect, requested, resolved, actual, result, reason, attribution.ToImmutable());
    }

    /// <summary>Removes a capability and its selectable command while preserving progression.</summary>
    private static int RemoveCapability(ActionCharacterState recipient, string id)
    {
        if (!recipient.Loadout.Actions.Remove(id)) return 0;
        recipient.Loadout.Commands.RemoveAll(command => command.Id.Value == id);
        return 1;
    }

    /// <summary>Sums supported signed additions and clamps once to the numerical field's permitted range.</summary>
    private double Modified(ActionCharacterState recipient, ActionEffect effect, NumericalField field, double amount, ImmutableArray<string>.Builder attribution)
    {
        double sum = 0;
        foreach (var rule in Rules(recipient))
            if (rule.Effect is ModifyEffect modify && modify.Field == field && modify.Filter.Verb == effect.Verb
                && (modify.Filter.DamageType is null || effect is DamageEffect damage && damage.Type == modify.Filter.DamageType))
            { sum += modify.Adjustment; attribution.Add(rule.Id); }
        return Math.Clamp(amount + sum, 0, field is NumericalField.DamageAmount or NumericalField.RestoreAmount or NumericalField.StatusDuration ? int.MaxValue : float.MaxValue / 2d);
    }

    /// <summary>Checks live exclusions without retroactively cancelling committed actions.</summary>
    private bool Restricted(ActionCharacterState source, ActionData action) => Rules(source).Any(rule => rule.Effect is RestrictEffect restrict && restrict.Filter.Matches(action, source.Id));

    /// <summary>Enumerates explicit rules with stable instance attribution.</summary>
    private static IEnumerable<(ActionEffect Effect, string Id)> Rules(ActionCharacterState character)
    {
        for (var index = 0; index < character.Rules.Count; index++) yield return (character.Rules[index], $"{character.Id}:equipment:{index}");
        foreach (var status in character.ActiveStatuses.OrderBy(status => status.Sequence))
            for (var index = 0; index < status.Definition.Rules.Length; index++)
                yield return (status.Definition.Rules[index], $"{status.Definition.Id}:{status.Sequence}:{index}:source={status.SourceId}");
    }

    /// <summary>Applies required duplicate handling and independent persistence state.</summary>
    private bool ApplyStatus(ActionCharacterState recipient, StatusEffectId id, string source, StatusPersistence persistence)
    {
        var definition = _statuses[id];
        var duplicates = recipient.ActiveStatuses.Where(status => status.Definition.Id == id).ToArray();
        if (duplicates.Length > 0 && definition.Duplicates == DuplicateStatusRule.Reject) return false;
        if (definition.Duplicates is DuplicateStatusRule.Refresh or DuplicateStatusRule.Replace)
            foreach (var existing in duplicates) recipient.ActiveStatuses.Remove(existing);
        var application = NextApplicationSequence();
        var sequence = definition.Duplicates == DuplicateStatusRule.Refresh && duplicates.Length > 0 ? duplicates[0].Sequence : application;
        var status = new ActiveStatus(sequence, definition, source, persistence, Combat.Timeline.CurrentTurn, recipient.TurnsStarted);
        status.ApplicationSequence = application;
        recipient.ActiveStatuses.Add(status);
        if (status.ExpiresAt is { } expiration && !Combat.HasEnded)
        {
            var recipientId = recipient.Id;
            Combat.ScheduleSimulationEvent(expiration, $"Expire {id}",
                system => system.Resolver!._characters[recipientId].ActiveStatuses.RemoveAll(instance => instance.ApplicationSequence == application), isVisible: false);
        }
        return true;
    }

    /// <summary>Expires recipient-turn statuses before selection and automatic decision making.</summary>
    private void OnTurnStarted(string id)
    {
        if (_characters.TryGetValue(id, out var recipient)) recipient.TurnsStarted++;
        Expire();
    }
    /// <summary>Removes expired instances without reversing any past changes.</summary>
    private void Expire()
    {
        foreach (var character in _characters.Values)
            character.ActiveStatuses.RemoveAll(status => status.ExpiresAt is { } time && time <= Combat.Timeline.CurrentTurn
                || status.ExpiresOnTurnStart is { } count && count <= character.TurnsStarted || status.RemainingUses == 0);
    }
    /// <summary>Validates definitions and resolves capability references before any mutation.</summary>
    private void ValidateContext(ActionData action)
    {
        ActionValidation.Validate(action, _statuses);
        foreach (var grant in action.BuildEffects().OfType<GrantEffect>())
            if (grant.Subject is GrantCapability capability && !ActionCatalog.ContainsKey(capability.ActionId)) throw new ArgumentException($"Unknown action capability '{capability.ActionId}'.");
    }
    /// <summary>Reads existing quantities without duplicating resource ownership.</summary>
    private static int Available(ActionCharacterState recipient, ConsumableSubject subject) => subject switch
    {
        ResourceSubject => recipient.Loadout.Focus,
        ItemQuantitySubject item => recipient.Loadout.Items.Find(owned => owned.Id == item.Item)?.Quantity ?? 0,
        _ => throw new ArgumentException("Unsupported quantity subject."),
    };
    /// <summary>Consumes at most what is available; costs are checked separately before commitment.</summary>
    private static int Consume(ActionCharacterState recipient, ConsumableSubject subject, int amount)
    {
        if (subject is ItemQuantitySubject item) return recipient.Loadout.ConsumeItem(item.Item, amount);
        var actual = Math.Min(recipient.Loadout.Focus, amount);
        recipient.Loadout.Focus -= actual;
        return actual;
    }
    /// <summary>Extracts the requested numerical value while retaining the complete typed request in outcomes.</summary>
    private static double Requested(ActionEffect effect) => effect switch
    {
        DamageEffect damage => damage.Amount, RestoreEffect restore => restore.Amount, DelayEffect delay => delay.Amount,
        ConsumeEffect consume => consume.Amount, GrantEffect { Subject: GrantResource resource } => resource.Amount,
        GrantEffect { Subject: GrantItem item } => item.Item.Quantity, ApplyEffect apply => apply.Persistence.Duration,
        DispelEffect dispel => dispel.Limit ?? int.MaxValue, TransferEffect transfer => transfer.Amount, _ => 1,
    };
    /// <summary>Maps authoritative formation position to scheduler row ordering.</summary>
    private static CombatRow Row(FormationSlot slot) => slot >= FormationSlot.FrontLeft ? CombatRow.Front : slot >= FormationSlot.MiddleLeft ? CombatRow.Middle : CombatRow.Back;
}
