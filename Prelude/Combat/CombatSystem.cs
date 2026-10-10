namespace NexusRealms.Prelude.Combat;

public enum CombatTeam { PlayerAndAllies, Enemies }
public enum CombatOutcome { Victory, Defeat, Escape }
public enum CombatRow { Front, Middle, Back }

public sealed class Combatant(string id, bool playerControlled, float turn = 0f,
    int initiative = 0, CombatTeam team = CombatTeam.PlayerAndAllies,
    CombatRow row = CombatRow.Front)
{
    public int Initiative { get; } = initiative;
    public CombatTeam Team { get; } = team;
    public CombatRow Row { get; } = row;
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
    public CombatTimeline Timeline { get; } = new();
    public Combatant? ActiveCombatant { get; private set; }
    public bool HasEnded { get; private set; }
    public CombatOutcome? Outcome { get; private set; }
    public Func<CombatSystem, bool>[] VictoryConditions { get; set; } = [];
    public Func<CombatSystem, bool>[] DefeatConditions { get; set; } = [];
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
        SubmitAction(new(1f, (_, _) => { }));
        return true;
    }
    public Func<CombatSystem, Combatant, CombatAction>? DecideAction { get; set; }
    public Func<CombatSystem, bool>? IsCombatOver { get; set; }
    public event Action? Changed;

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

    public bool CancelEvent(long id)
    {
        _effects.Remove(id);
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
            var next = Timeline.Advance();
            if (next is null) break;
            if (next.CombatantId is { } id)
            {
                _turns.Remove(id);
                ActiveCombatant = _combatants[id];
                if (EvaluateOutcome()) break;
                if (id == _escapingActor) { End(CombatOutcome.Escape); break; }
                Changed?.Invoke();
                if (ActiveCombatant.PlayerControlled) break;
                if (DecideAction is null) break;
                ResolveAction(DecideAction(this, ActiveCombatant));
            }
            else if (_effects.Remove(next.Id, out var effect)) effect(this);
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
        Process();
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
        try { action.Resolve(this, actor); }
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
    }

    public void End(CombatOutcome? outcome = null)
    {
        if (HasEnded) return;
        Outcome = outcome;
        HasEnded = true;
        ActiveCombatant = null;
        _turns.Clear();
        _effects.Clear();
        Timeline.Clear();
        Changed?.Invoke();
        if (outcome is { } result) Ended?.Invoke(result);
    }

    private void EnsureRunning()
    {
        if (HasEnded) throw new InvalidOperationException("The encounter has ended.");
    }
}


