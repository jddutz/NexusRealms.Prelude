namespace NexusRealms.Prelude.Combat;

public sealed class Combatant(string id, bool playerControlled, float turn = 0f,
    int initiative = 0, CombatTeam team = CombatTeam.PlayerAndAllies,
    CombatRow row = CombatRow.Front)
{
    public int Initiative { get; } = initiative;
    public CombatTeam Team { get; } = team;
    public CombatRow Row { get; internal set; } = row;
    public uint RandomRank { get; internal set; }
    public string Id { get; } = id;
    public bool PlayerControlled { get; } = playerControlled;
    public float Turn { get; internal set; } = turn;
}

public sealed record CombatAction(float TurnCost, Action<CombatSystem, Combatant> Resolve);

/// <summary>Resolves occurrences until input is needed. Effects share the combatant timeline.</summary>
public sealed class CombatSystem
{
    private readonly Dictionary<string, Combatant> _combatants = [];
    private readonly Dictionary<string, long> _turns = [];
    private readonly Dictionary<long, Action<CombatSystem>> _effects = [];
    private readonly HashSet<long> _simulationEvents = [];
    internal NexusRealms.Prelude.Actions.CombatActionResolver? Resolver { get; set; }
    internal bool SuppressPump { get; set; }
    /// <summary>After effects, outcome checking, and rescheduling; before the next occurrence.</summary>
    public event Action<string>? ActionCommitted;
    public event Action<string>? ActionCommitting;
    public event Action? EventResolved;
    internal bool LastCommitHadAction { get; private set; }
    private bool _noLegalAction;
    public CombatTimeline Timeline { get; } = new();
    public Combatant? ActiveCombatant { get; private set; }
    public bool HasEnded { get; private set; }
    public CombatOutcome? Outcome { get; private set; }
    /// <summary>Optional exclusive timeline boundary. Occurrences at this turn do not resolve.</summary>
    public float? TurnLimit { get; init; }
    public Func<CombatSystem, bool>[] VictoryConditions { get; set; } = [];
    public Func<CombatSystem, bool>[] DefeatConditions { get; set; } = [];
    private Func<CombatSystem, bool>[]? _safeVictory;
    private Func<CombatSystem, bool>[]? _safeDefeat;
    public void ConfigureObjectives(IEnumerable<CombatConditionData> victory, IEnumerable<CombatConditionData> defeat)
    {
        VictoryConditions = victory.Select(ScenarioRules.Build).ToArray();
        DefeatConditions = defeat.Select(ScenarioRules.Build).ToArray();
        _safeVictory = VictoryConditions.ToArray();
        _safeDefeat = DefeatConditions.ToArray();
    }
    public Func<string, bool>? IsCombatantAlive { get; set; }
    public bool IsAlive(string id) => Contains(id) && (IsCombatantAlive?.Invoke(id) ?? true);
    public bool HasLivingEnemies => _combatants.Values.Any(actor => actor.Team == CombatTeam.Enemies && IsAlive(actor.Id));
    private string? _escapingActor;
    public bool IsEscaping => _escapingActor is not null && !HasEnded;
    public event Action<CombatOutcome>? Ended;

    public bool EvaluateOutcome()
    {
        if (HasEnded) return true;
        if (DefeatConditions.Any(condition => condition(this))) End(CombatOutcome.Defeat);
        else if (VictoryConditions.Length > 0 && VictoryConditions.All(condition => condition(this))) End(CombatOutcome.Victory);
        else if (IsCombatOver?.Invoke(this) == true) End();
        return HasEnded;
    }

    public bool RequestEscape(string actorId)
    {
        if (HasEnded || _resolving || IsEscaping || ActiveCombatant is not { PlayerControlled: true } actor
            || actor.Id != actorId || EvaluateOutcome()) return false;
        _escapingActor = actorId;
        SubmitAction(new(1f, (system, actor) => system.Resolver?.RecordEmptyAction(actor.Id, "escape", 1f)));
        return true;
    }
    public Func<CombatSystem, Combatant, CombatAction>? DecideAction { get; set; }
    public Func<CombatSystem, bool>? IsCombatOver { get; set; }
    public event Action? Changed;
    /// <summary>Raised at the start of a character's turn before input or automatic decisions.</summary>
    public event Action<string>? TurnStarted;

    /// <summary>Delays only an existing future occurrence, never an action currently resolving.</summary>
    public bool TryDelay(string id, float amount)
    {
        if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (HasEnded || !_turns.ContainsKey(id) || !IsAlive(id) || amount == 0) return false;
        var next = _combatants[id].Turn + amount;
        if (!float.IsFinite(next) || next <= _combatants[id].Turn) return false;
        AdjustTurn(id, amount);
        return true;
    }

    /// <summary>Updates row-based tie breaking after an authored formation move.</summary>
    public void Relocate(string id, CombatRow row)
    {
        if (!Enum.IsDefined(row)) throw new ArgumentOutOfRangeException(nameof(row));
        _combatants[id].Row = row;
        if (_turns.Remove(id, out var turn))
        {
            Timeline.Cancel(turn);
            ScheduleTurn(_combatants[id]);
        }
        Changed?.Invoke();
    }

    private bool _resolving;
    public bool IsResolving => _resolving;
    /// <summary>Yields until just after the next living character, skipping non-character events.</summary>
    public float? WaitCost(string actorId)
    {
        if (HasEnded || ActiveCombatant is not { } actor || actor.Id != actorId || !IsAlive(actorId)) return null;
        var next = Timeline.TurnOrder.FirstOrDefault(entry => entry.CombatantId is { } id
            && id != actorId && IsAlive(id));
        if (next is null) return null;
        var destination = next.Turn + 0.01f;
        var cost = destination - actor.Turn;
        return float.IsFinite(destination) && destination > next.Turn && cost > 0f && float.IsFinite(cost) ? cost : null;
    }
    private uint _randomState;
    public CombatSystem(uint seed = 1)
    {
        _randomState = seed;
        Timeline.Changed += () => Changed?.Invoke();
    }

    public void Add(Combatant combatant)
    {
        EnsureRunning();
        CombatTimeline.Validate(combatant.Turn);
        _combatants.Add(combatant.Id, combatant);
        _randomState = unchecked(_randomState * 1664525u + 1013904223u);
        combatant.RandomRank = _randomState;
        ScheduleTurn(combatant);
    }

    private void ScheduleTurn(Combatant combatant)
    {
        combatant.Turn = Math.Max(Timeline.CurrentTurn, combatant.Turn);
        _turns[combatant.Id] = Timeline.Schedule(combatant.Turn, combatant.Id,
            TimelinePriority.Combatant, combatant.Id, initiative: combatant.Initiative,
            team: combatant.Team, row: combatant.Row, randomRank: combatant.RandomRank);
    }

    public bool Contains(string id) => _combatants.ContainsKey(id);

    public void Remove(string id)
    {
        _combatants.Remove(id);
        if (_turns.Remove(id, out var turn)) Timeline.Cancel(turn);
        if (ActiveCombatant?.Id == id) ActiveCombatant = null;
        Changed?.Invoke();
    }

    public void AdjustTurn(string id, float adjustment)
    {
        EnsureRunning();
        CombatTimeline.Validate(adjustment);
        var combatant = _combatants[id];
        var turn = combatant.Turn + adjustment;
        CombatTimeline.Validate(turn);
        combatant.Turn = Math.Max(Timeline.CurrentTurn, turn);
        if (_turns.TryGetValue(id, out var occurrence)) Timeline.Move(occurrence, combatant.Turn);
        Changed?.Invoke();
    }

    public long ScheduleEvent(float turn, string label, Action<CombatSystem> resolve,
        int priority = TimelinePriority.Effect, string? icon = null,
        bool isVisible = true, string? presentationKey = null)
    {
        EnsureRunning();
        ArgumentNullException.ThrowIfNull(resolve);
        var id = Timeline.Schedule(turn, label, priority, icon: icon,
            isVisible: isVisible, presentationKey: presentationKey);
        _effects.Add(id, resolve);
        return id;
    }

    /// <summary>Schedules a callback whose mutable owners are obtained only from its combat argument.</summary>
    internal long ScheduleSimulationEvent(float turn, string label, Action<CombatSystem> resolve,
        int priority = TimelinePriority.Effect, string? icon = null, bool isVisible = true, string? presentationKey = null)
    {
        var id = ScheduleEvent(turn, label, resolve, priority, icon, isVisible, presentationKey);
        _simulationEvents.Add(id);
        return id;
    }

    public bool CancelEvent(long id)
    {
        _effects.Remove(id);
        _simulationEvents.Remove(id);
        return Timeline.Cancel(id);
    }

    /// <summary>A bounded pump prevents authored same-time events from hanging the caller.</summary>
    public void Process(int maxOccurrences = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOccurrences);
        if (EvaluateOutcome()) return;
        for (var i = 0; i < maxOccurrences && !HasEnded && ActiveCombatant is null; i++)
        {
            if (EvaluateOutcome()) break;
            if (TurnLimit is { } limit && Timeline.Preview(1).FirstOrDefault() is { } pending && pending.Turn >= limit)
            {
                End(CombatOutcome.Draw);
                break;
            }
            var next = Timeline.Advance();
            if (next is null) break;
            if (next.CombatantId is { } id)
            {
                _turns.Remove(id);
                ActiveCombatant = _combatants[id];
                TurnStarted?.Invoke(id);
                if (EvaluateOutcome()) break;
                if (id == _escapingActor) { End(CombatOutcome.Escape); break; }
                Changed?.Invoke();
                if (ActiveCombatant.PlayerControlled) break;
                if (DecideAction is null) break;
                ResolveAction(DecideAction(this, ActiveCombatant));
            }
            else if (_effects.Remove(next.Id, out var effect))
            {
                _simulationEvents.Remove(next.Id);
                effect(this);
                EventResolved?.Invoke();
            }
        }
        Changed?.Invoke();
    }

    public void SubmitAction(CombatAction action)
    {
        EnsureRunning();
        if (_resolving) throw new InvalidOperationException("An action is already resolving.");
        if (ActiveCombatant is not { PlayerControlled: true })
            throw new InvalidOperationException("No player action is pending.");
        ResolveAction(action);
        if (!SuppressPump) Process();
    }

    public void SubmitAutomaticAction(CombatAction action)
    {
        EnsureRunning();
        if (_resolving || ActiveCombatant is not { PlayerControlled: false })
            throw new InvalidOperationException("No automatic action is pending.");
        ResolveAction(action);
        if (!SuppressPump) Process();
    }

    /// <summary>Existing unavailable-action rule: spend one timeline unit, with no invented voluntary action.</summary>
    public void SubmitNoLegalAction()
    {
        var operation = new CombatAction(1f, (_, _) => { });
        _noLegalAction = true;
        try { if (ActiveCombatant?.PlayerControlled == true) SubmitAction(operation); else SubmitAutomaticAction(operation); }
        finally { _noLegalAction = false; }
    }

    // A declared local threat assumption, on an isolated copy only: act now, with no
    // intervening events. It is not a prediction of the player's next input.
    internal void OpenThreatProbe(string id)
    {
        if (!SuppressPump) throw new InvalidOperationException("Threat probes require isolated simulation state.");
        if (_turns.Remove(id, out var occurrence)) Timeline.Cancel(occurrence);
        ActiveCombatant = _combatants[id];
        ActiveCombatant.Turn = Timeline.CurrentTurn;
    }

    private void ResolveAction(CombatAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(action.Resolve);
        CombatTimeline.Validate(action.TurnCost);
        var actor = ActiveCombatant ?? throw new InvalidOperationException("No active combatant.");
        var next = actor.Turn + action.TurnCost;
        if (action.TurnCost <= 0f || !float.IsFinite(next) || next <= actor.Turn)
            throw new ArgumentOutOfRangeException(nameof(action), "Cost must advance the float timeline.");
        _resolving = true;
        try { ActionCommitting?.Invoke(actor.Id); action.Resolve(this, actor); }
        finally { _resolving = false; }
        ActiveCombatant = null;
        EvaluateOutcome();
        if (!HasEnded && _combatants.ContainsKey(actor.Id))
        {
            next = actor.Turn + action.TurnCost;
            CombatTimeline.Validate(next);
            if (next <= actor.Turn) throw new InvalidOperationException("Cost no longer advances the timeline.");
            actor.Turn = next;
            ScheduleTurn(actor);
        }
        Changed?.Invoke();
        LastCommitHadAction = !_noLegalAction;
        ActionCommitted?.Invoke(actor.Id);
    }

    public void End(CombatOutcome? outcome = null)
    {
        if (HasEnded) return;
        Outcome = outcome;
        HasEnded = true;
        ActiveCombatant = null;
        _turns.Clear();
        _effects.Clear();
        _simulationEvents.Clear();
        Timeline.Clear();
        Changed?.Invoke();
        if (outcome is { } result) Ended?.Invoke(result);
    }

    private void EnsureRunning()
    {
        if (HasEnded) throw new InvalidOperationException("The encounter has ended.");
    }

    internal CombatSystem CopyForPreview()
    {
        if (_effects.Keys.Any(id => !_simulationEvents.Contains(id)))
            throw new InvalidOperationException("Combat has an opaque event callback; use resolver.ScheduleEvent for AI simulation.");
        if (IsCombatOver is not null) throw new InvalidOperationException("AI simulation requires data-only encounter objectives.");
        if (VictoryConditions.Length > 0 && (_safeVictory is null || !VictoryConditions.SequenceEqual(_safeVictory))
            || DefeatConditions.Length > 0 && (_safeDefeat is null || !DefeatConditions.SequenceEqual(_safeDefeat)))
            throw new InvalidOperationException("AI simulation requires ConfigureObjectives, not opaque objective predicates.");
        var copy = new CombatSystem(_randomState) { TurnLimit = TurnLimit, SuppressPump = true,
            VictoryConditions = VictoryConditions.ToArray(), DefeatConditions = DefeatConditions.ToArray(),
            HasEnded = HasEnded, Outcome = Outcome, _escapingActor = _escapingActor };
        copy._safeVictory = copy.VictoryConditions.ToArray();
        copy._safeDefeat = copy.DefeatConditions.ToArray();
        Timeline.CopyTo(copy.Timeline);
        foreach (var (id, actor) in _combatants)
            copy._combatants.Add(id, new(id, actor.PlayerControlled, actor.Turn, actor.Initiative, actor.Team, actor.Row) { RandomRank = actor.RandomRank });
        foreach (var pair in _turns) copy._turns.Add(pair.Key, pair.Value);
        foreach (var pair in _effects) copy._effects.Add(pair.Key, pair.Value);
        copy._simulationEvents.UnionWith(_simulationEvents);
        if (ActiveCombatant is { } active) copy.ActiveCombatant = copy._combatants[active.Id];
        return copy;
    }
}


