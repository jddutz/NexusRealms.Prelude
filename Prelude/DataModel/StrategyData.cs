namespace NexusRealms.Prelude.DataModel;

public enum StrategyId { Aggressive, Protective, Disruptive, Opportunistic, Chaotic, Reactive, Analytical, Defensive, Supportive, Cautious, Reckless }

/// <summary>Version one policies and typed settings authored on a CharacterData.</summary>
public sealed record StrategyAssignment(StrategyId Strategy = StrategyId.Aggressive, string[]? ProtectedIds = null)
{
    public int Version { get; init; } = 1;
    public StrategyId Fallback { get; init; } = StrategyId.Aggressive;
    public ImmutableArray<StrategyId> Modes { get; init; } = [StrategyId.Aggressive, StrategyId.Disruptive, StrategyId.Defensive];
    public int InitialMode { get; init; }
    public int HistoryWindow { get; init; } = 4;
    public int DecisionDepth { get; init; } = 3;
    public int NodeBudget { get; init; } = 128;
    public StrategyId Continuation { get; init; } = StrategyId.Aggressive;
    public StrategyId OpposingContinuation { get; init; } = StrategyId.Aggressive;
    public bool ProtectAllies { get; init; }
    public StrategyWeights? Weights { get; init; }

    public void Validate()
    {
        if (Version != 1 || !Enum.IsDefined(Strategy) || !IsImmediate(Fallback)
            || !IsImmediate(Continuation) || !IsImmediate(OpposingContinuation) || Continuation == StrategyId.Protective || OpposingContinuation == StrategyId.Protective
            || HistoryWindow <= 0 || DecisionDepth <= 0 || NodeBudget <= 0
            || Modes.IsDefaultOrEmpty || Modes.Any(mode => !IsImmediate(mode)) || InitialMode < 0 || InitialMode >= Modes.Length)
            throw new ArgumentException("Invalid strategy settings or unsupported strategy version.");
        if ((Strategy == StrategyId.Protective || Fallback == StrategyId.Protective || Modes.Contains(StrategyId.Protective))
            && !ProtectAllies && (ProtectedIds is null || ProtectedIds.Length == 0))
            throw new ArgumentException("Protective requires protected characters or the explicit allied group selector.");
        if (Weights is { } weights && weights.Values().Any(value => value < 0 || value > 10000))
            throw new ArgumentException("Strategy weights must be integers between zero and 10000.");
    }
    public static bool IsImmediate(StrategyId id) => Enum.IsDefined(id) && id is not (StrategyId.Analytical or StrategyId.Chaotic or StrategyId.Reactive);
}

public sealed record StrategyWeights(int Damage = 100, int Elimination = 1000, int SelfPreservation = 100,
    int AllyPreservation = 100, int WardPreservation = 100, int Denial = 50, int Support = 25,
    int Opening = 0, int Focus = 25, int Item = 50, int Time = 100)
{
    internal IEnumerable<int> Values() => [Damage, Elimination, SelfPreservation, AllyPreservation, WardPreservation, Denial, Support, Opening, Focus, Item, Time];
    public static StrategyWeights For(StrategyId strategy) => strategy switch
    {
        StrategyId.Protective => new(WardPreservation: 600),
        StrategyId.Defensive => new(SelfPreservation: 500),
        StrategyId.Disruptive => new(Denial: 400),
        StrategyId.Opportunistic => new(Elimination: 1800, Opening: 300),
        StrategyId.Supportive => new(AllyPreservation: 300, Support: 200),
        StrategyId.Cautious => new(Focus: 200, Item: 300),
        StrategyId.Reckless => new(Damage: 200, SelfPreservation: 0, Focus: 0, Item: 0, Time: 25),
        _ => new(),
    };
}

public enum StrategyConditionKind { HealthAtMost, CharacterDefeated, TurnAtLeast }
[Flags]
public enum StrategyBoundary { TurnStarted = 1, ActionCommitted = 2, EventResolved = 4, All = 7 }
public sealed record StrategyCondition(StrategyConditionKind Kind, string SubjectId = "$self", int Health = 0, float Turn = 0);
public sealed record StrategyTransition(string Id, StrategyCondition Condition, StrategyAssignment Destination)
{
    public int Priority { get; init; }
    public StrategyBoundary Boundary { get; init; } = StrategyBoundary.All;
    public ImmutableArray<string> Actors { get; init; } = ["$self"];
    public bool RepeatOnRisingEdge { get; init; }
    public float Cooldown { get; init; }
    public bool ClearModeMemory { get; init; }
    public bool ClearHistory { get; init; }
}

