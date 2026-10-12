using NexusRealms.Prelude.Actions;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

public sealed record StrategyOverride(string Id, StrategyAssignment Assignment, int Priority, string Source,
    long Sequence, float? ExpiresAt = null, int? RemainingCommits = null);
public sealed record ObservedEffect(string RecipientId, EffectVerb Verb, DamageType? DamageType, double RequestedAmount, double ActualChange);
public sealed record ObservedAction(long Sequence, float Turn, string SourceId, string ActionId,
    ImmutableArray<string> Classifications, ImmutableArray<ObservedEffect> Effects);
public sealed record TransitionMemory(bool Fired, bool LastCondition, float? LastTurn);
public sealed record DecisionMemorySave(ImmutableSortedDictionary<string, StrategyAssignment> Assignments,
    ImmutableSortedDictionary<string, int> Modes, ImmutableSortedDictionary<string, ImmutableArray<StrategyOverride>> Overrides,
    ImmutableSortedDictionary<string, ImmutableArray<ObservedAction>> Histories,
    ImmutableSortedDictionary<string, TransitionMemory> Transitions, long Sequence, long ApplicationSequence);
public sealed record TransitionTrace(string Id, string ActorId, StrategyId From, StrategyId To);
public sealed record BehaviorSnapshot(string ActorId, StrategyAssignment Base, StrategyAssignment Effective,
    string? OverrideId, int Mode, ImmutableArray<ObservedAction> History, ImmutableArray<TransitionTrace> Transitions);

/// <summary>Per-character runtime memory. Definitions and placements own no mutable decision state.</summary>
public sealed class EncounterBehavior
{
    private readonly CombatActionResolver _resolver;
    private readonly Dictionary<string, StrategyAssignment> _assignments = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _modes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<StrategyOverride>> _overrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ObservedAction>> _histories = new(StringComparer.Ordinal);
    private readonly List<(string Owner, StrategyTransition Definition)> _transitions = [];
    private readonly Dictionary<string, (bool Fired, bool LastCondition, float LastTurn)> _transitionMemory = new(StringComparer.Ordinal);
    private ImmutableArray<TransitionTrace> _lastTransitions = [];
    private readonly List<ActionOutcome> _pending = [];
    private long _sequence;
    private BehaviorSnapshot? _committing;
    private ImmutableHashSet<long> _committingOverrides = [];
    internal EncounterBehavior(CombatActionResolver resolver)
    {
        _resolver = resolver;
        resolver.Resolved += outcome => { if (resolver.Combat.IsResolving) _pending.Add(outcome); };
        resolver.Combat.ActionCommitted += OnCommitted;
        resolver.Combat.ActionCommitting += id =>
        {
            _committing = Snapshot(id);
            _committingOverrides = (_overrides.GetValueOrDefault(id) ?? []).Select(entry => entry.Sequence).ToImmutableHashSet();
        };
        resolver.Combat.TurnStarted += _ => ApplyBoundary(StrategyBoundary.TurnStarted);
        resolver.Combat.EventResolved += () => ApplyBoundary(StrategyBoundary.EventResolved);
    }
    public void Assign(string actorId, StrategyAssignment assignment)
    {
        assignment = _resolver.BindAssignment(actorId, assignment);
        ValidateAssignment(actorId, assignment);
        _assignments[actorId] = Freeze(assignment);
    }
    private static StrategyAssignment Freeze(StrategyAssignment assignment) => assignment with { ProtectedIds = assignment.ProtectedIds?.ToArray() };
    public void AssignBatch(IReadOnlyDictionary<string, StrategyAssignment> assignments)
    {
        var bound = assignments.ToDictionary(pair => pair.Key, pair => _resolver.BindAssignment(pair.Key, pair.Value));
        foreach (var pair in bound) ValidateAssignment(pair.Key, pair.Value);
        foreach (var pair in bound) _assignments[pair.Key] = Freeze(pair.Value);
    }
    private void ValidateAssignment(string actorId, StrategyAssignment assignment)
    {
        assignment.Validate();
        if (!_resolver.Characters.TryGetValue(actorId, out var actor)) throw new ArgumentException("Unknown strategy actor.");
        if (assignment.ProtectedIds?.Any(id => !_resolver.Characters.TryGetValue(id, out var ward) || ward.Team != actor.Team) == true)
            throw new ArgumentException("Protected characters must identify existing allies.");
    }
    public void AddTransition(string owner, StrategyTransition transition)
    {
        var id = owner + "/" + transition.Id;
        if (string.IsNullOrWhiteSpace(transition.Id) || _transitions.Any(entry => entry.Owner + "/" + entry.Definition.Id == id)
            || !Enum.IsDefined(transition.Condition.Kind) || !float.IsFinite(transition.Condition.Turn)
            || transition.Condition.Turn < 0 || transition.Condition.Health < 0
            || transition.Boundary == 0 || (transition.Boundary & ~StrategyBoundary.All) != 0
            || !float.IsFinite(transition.Cooldown) || transition.Cooldown < 0 || transition.Actors.IsDefaultOrEmpty)
            throw new ArgumentException("Invalid or duplicate strategy transition.");
        foreach (var actor in transition.Actors) ValidateAssignment(Resolve(owner, actor), transition.Destination);
        if (!_resolver.Characters.ContainsKey(Resolve(owner, transition.Condition.SubjectId))) throw new ArgumentException("Unknown transition subject.");
        _transitions.Add((owner, transition));
    }
    public void AddOverride(string actorId, string id, StrategyAssignment assignment, int priority, string source,
        float? expiresAt = null, int? commits = null)
    {
        assignment = _resolver.BindAssignment(actorId, assignment);
        ValidateAssignment(actorId, assignment);
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(source) || expiresAt is null && commits is null
            || expiresAt is { } time && (!float.IsFinite(time) || time <= _resolver.Combat.Timeline.CurrentTurn) || commits is <= 0)
            throw new ArgumentException("Overrides require an identity, source and future expiration or positive commit count.");
        if (!_overrides.TryGetValue(actorId, out var overrides)) _overrides[actorId] = overrides = [];
        overrides.RemoveAll(entry => entry.Id == id);
        overrides.Add(new(id, Freeze(assignment), priority, source, _resolver.NextApplicationSequence(), expiresAt, commits));
    }
    public void RemoveOverride(string actorId, string id)
    {
        if (_overrides.TryGetValue(actorId, out var overrides)) overrides.RemoveAll(entry => entry.Id == id);
    }
    public BehaviorSnapshot Snapshot(string actorId, StrategyAssignment? supplied = null)
    {
        var assignment = _resolver.BindAssignment(actorId, supplied ?? _assignments.GetValueOrDefault(actorId) ?? new());
        ValidateAssignment(actorId, assignment);
        var statuses = _resolver.Characters[actorId].Statuses.Where(status => status.Definition.StrategyOverride is not null)
            .Select(status => new StrategyOverride($"status:{status.Sequence}", status.Definition.StrategyOverride!,
                status.Definition.StrategyPriority, status.SourceId, status.ApplicationSequence));
        var chosen = (_overrides.GetValueOrDefault(actorId) ?? []).Where(entry => entry.ExpiresAt is null || entry.ExpiresAt > _resolver.Combat.Timeline.CurrentTurn)
            .Concat(statuses).OrderByDescending(entry => entry.Priority).ThenByDescending(entry => entry.Sequence).ThenBy(entry => entry.Id, StringComparer.Ordinal).FirstOrDefault();
        var effective = _resolver.BindAssignment(actorId, chosen?.Assignment ?? assignment);
        ValidateAssignment(actorId, effective);
        return new(actorId, assignment, effective, chosen?.Id, _modes.GetValueOrDefault(actorId, effective.InitialMode),
            (_histories.GetValueOrDefault(actorId) ?? []).TakeLast(effective.HistoryWindow).ToImmutableArray(), _lastTransitions);
    }
    public void ApplyBoundary(StrategyBoundary boundary = StrategyBoundary.All)
    {
        var turn = _resolver.Combat.Timeline.CurrentTurn;
        foreach (var list in _overrides.Values) list.RemoveAll(entry => entry.ExpiresAt <= turn || entry.RemainingCommits == 0);
        var eligible = new List<(string Id, string Actor, StrategyTransition Transition)>();
        foreach (var (owner, transition) in _transitions.OrderBy(entry => entry.Owner + "/" + entry.Definition.Id, StringComparer.Ordinal))
        {
            if ((transition.Boundary & boundary) == 0) continue;
            var id = owner + "/" + transition.Id;
            var memory = _transitionMemory.GetValueOrDefault(id, (Fired: false, LastCondition: false, LastTurn: float.NegativeInfinity));
            var subject = _resolver.Characters[Resolve(owner, transition.Condition.SubjectId)];
            var condition = transition.Condition.Kind switch
            {
                StrategyConditionKind.HealthAtMost => subject.Health.Health <= transition.Condition.Health,
                StrategyConditionKind.CharacterDefeated => subject.Health.Health == 0,
                StrategyConditionKind.TurnAtLeast => turn >= transition.Condition.Turn,
                _ => throw new ArgumentException("Unknown transition condition."),
            };
            var fires = condition && (!memory.Fired || transition.RepeatOnRisingEdge && !memory.LastCondition)
                && turn - memory.LastTurn >= transition.Cooldown;
            _transitionMemory[id] = (memory.Fired || fires, condition, fires ? turn : memory.LastTurn);
            if (fires) foreach (var actor in transition.Actors) eligible.Add((id, Resolve(owner, actor), transition));
        }
        var changes = ImmutableArray.CreateBuilder<TransitionTrace>();
        // Compute all winners against the same post-resolution snapshot, then apply once.
        foreach (var group in eligible.GroupBy(entry => entry.Actor).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var winner = group.OrderByDescending(entry => entry.Transition.Priority).ThenBy(entry => entry.Id, StringComparer.Ordinal).First();
            var previous = _assignments.GetValueOrDefault(group.Key) ?? new();
            _assignments[group.Key] = Freeze(winner.Transition.Destination);
            if (winner.Transition.ClearModeMemory) _modes.Remove(group.Key);
            if (winner.Transition.ClearHistory) _histories.Remove(group.Key);
            changes.Add(new(winner.Id, group.Key, previous.Strategy, winner.Transition.Destination.Strategy));
        }
        _lastTransitions = _lastTransitions.AddRange(changes);
    }
    private void OnCommitted(string actorId)
    {
        if (_resolver.Combat.LastCommitHadAction)
        {
            var snapshot = _committing ?? Snapshot(actorId);
            if (snapshot.Effective.Strategy == StrategyId.Chaotic)
                _modes[actorId] = (snapshot.Mode + 1) % snapshot.Effective.Modes.Length;
            foreach (var outcome in _pending)
            {
                var observed = new ObservedAction(++_sequence, _resolver.Combat.Timeline.CurrentTurn, outcome.SourceId, outcome.Action.Id,
                    outcome.Action.Classifications, outcome.Effects.Select(effect => new ObservedEffect(effect.RecipientId, effect.Verb,
                        (effect.Requested as DamageEffect)?.Type, effect.RequestedAmount, effect.ActualChange)).ToImmutableArray());
                foreach (var id in _resolver.Characters.Keys)
                {
                    if (!_histories.TryGetValue(id, out var history)) _histories[id] = history = [];
                    history.Add(observed);
                }
            }
            if (_overrides.TryGetValue(actorId, out var overrides))
                for (var index = 0; index < overrides.Count; index++)
                    if (_committingOverrides.Contains(overrides[index].Sequence) && overrides[index].RemainingCommits is { } remaining)
                        overrides[index] = overrides[index] with { RemainingCommits = remaining - 1 };
        }
        _pending.Clear();
        _committing = null;
        _committingOverrides = [];
        _lastTransitions = [];
        ApplyBoundary(StrategyBoundary.ActionCommitted);
    }
    internal void CopyTo(EncounterBehavior copy)
    {
        foreach (var pair in _assignments) copy._assignments.Add(pair.Key, Freeze(pair.Value));
        foreach (var pair in _modes) copy._modes.Add(pair.Key, pair.Value);
        foreach (var pair in _overrides) copy._overrides.Add(pair.Key, pair.Value.Select(entry => entry with { Assignment = Freeze(entry.Assignment) }).ToList());
        foreach (var pair in _histories) copy._histories.Add(pair.Key, pair.Value.ToList());
        copy._transitions.AddRange(_transitions);
        foreach (var pair in _transitionMemory) copy._transitionMemory.Add(pair.Key, pair.Value);
        copy._sequence = _sequence;
        copy._lastTransitions = _lastTransitions;
    }
    public DecisionMemorySave Save() => new(_assignments.ToImmutableSortedDictionary(StringComparer.Ordinal),
        _modes.ToImmutableSortedDictionary(StringComparer.Ordinal),
        _overrides.ToImmutableSortedDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray(), StringComparer.Ordinal),
        _histories.ToImmutableSortedDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray(), StringComparer.Ordinal),
        _transitionMemory.ToImmutableSortedDictionary(pair => pair.Key,
            pair => new TransitionMemory(pair.Value.Fired, pair.Value.LastCondition, float.IsFinite(pair.Value.LastTurn) ? pair.Value.LastTurn : null), StringComparer.Ordinal),
        _sequence, _resolver.ApplicationSequence);
    public void Load(DecisionMemorySave saved)
    {
        foreach (var pair in saved.Assignments) ValidateAssignment(pair.Key, pair.Value);
        foreach (var pair in saved.Overrides) foreach (var entry in pair.Value) ValidateAssignment(pair.Key, entry.Assignment);
        if (saved.Modes.Any(pair => !_resolver.Characters.ContainsKey(pair.Key) || pair.Value < 0)
            || saved.Histories.Any(pair => !_resolver.Characters.ContainsKey(pair.Key)
                || pair.Value.Any(entry => !_resolver.Characters.ContainsKey(entry.SourceId)))
            || saved.Transitions.Keys.Any(id => !_transitions.Any(entry => entry.Owner + "/" + entry.Definition.Id == id))
            || saved.Sequence < 0 || saved.ApplicationSequence < 0)
            throw new ArgumentException("Saved AI memory does not match this encounter.");
        _assignments.Clear(); foreach (var pair in saved.Assignments) _assignments.Add(pair.Key, Freeze(pair.Value));
        _modes.Clear(); foreach (var pair in saved.Modes) _modes.Add(pair.Key, pair.Value);
        _overrides.Clear(); foreach (var pair in saved.Overrides) _overrides.Add(pair.Key, pair.Value.ToList());
        _histories.Clear(); foreach (var pair in saved.Histories) _histories.Add(pair.Key, pair.Value.ToList());
        _transitionMemory.Clear(); foreach (var pair in saved.Transitions)
            _transitionMemory.Add(pair.Key, (pair.Value.Fired, pair.Value.LastCondition, pair.Value.LastTurn ?? float.NegativeInfinity));
        _sequence = saved.Sequence;
        _resolver.RestoreApplicationSequence(saved.ApplicationSequence);
        _pending.Clear();
        _lastTransitions = [];
    }
    private static string Resolve(string owner, string id) => id == "$self" ? owner : id;
}

