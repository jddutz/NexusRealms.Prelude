namespace NexusRealms.Prelude.Combat;

/// <summary>Lower priorities execute first at exactly equal timeline positions.</summary>
public static class TimelinePriority
{
    public const int Effect = 0;
    public const int Combatant = 100;
}

public sealed record TimelineOccurrence(long Id, float Turn, int Priority, long Sequence,
    string Label, string? CombatantId = null, string? Icon = null,
    bool IsVisible = true, string? PresentationKey = null,
    int Initiative = 0, CombatTeam Team = CombatTeam.PlayerAndAllies,
    CombatRow Row = CombatRow.Front, uint RandomRank = 0);

/// <summary>Encounter-owned deterministic scheduling; no clock or rendering dependencies.</summary>
public sealed class CombatTimeline
{
    private readonly Dictionary<long, TimelineOccurrence> _pending = [];
    private long _sequence;
    public float CurrentTurn { get; private set; }
    public event Action? Changed;
    public IReadOnlyList<TimelineOccurrence> TurnOrder => Preview(int.MaxValue);

    public IReadOnlyList<TimelineOccurrence> Preview(int count, bool visibleOnly = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return _pending.Values.Where(x => !visibleOnly || x.IsVisible).OrderBy(x => x.Turn).ThenBy(x => x.Priority)
            .ThenByDescending(x => x.Initiative).ThenBy(x => x.Team).ThenBy(x => x.Row)
            .ThenBy(x => x.RandomRank).ThenBy(x => x.Sequence).Take(count).ToArray();
    }

    public long Schedule(float turn, string label, int priority = TimelinePriority.Effect,
        string? combatantId = null, string? icon = null,
        bool isVisible = true, string? presentationKey = null,
        int initiative = 0, CombatTeam team = CombatTeam.PlayerAndAllies,
        CombatRow row = CombatRow.Front, uint randomRank = 0)
    {
        Validate(turn);
        var id = checked(++_sequence);
        _pending.Add(id, new(id, Math.Max(CurrentTurn, turn), priority, id, label, combatantId, icon, isVisible, presentationKey,
            initiative, team, row, randomRank));
        Changed?.Invoke();
        return id;
    }

    public void SetVisibility(long id, bool isVisible)
    {
        _pending[id] = _pending[id] with { IsVisible = isVisible };
        Changed?.Invoke();
    }

    public bool Cancel(long id)
    {
        if (!_pending.Remove(id)) return false;
        Changed?.Invoke();
        return true;
    }

    public void Move(long id, float turn)
    {
        Validate(turn);
        var occurrence = _pending[id];
        _pending[id] = occurrence with { Turn = Math.Max(CurrentTurn, turn) };
        Changed?.Invoke();
    }

    public void Adjust(long id, float adjustment)
    {
        Validate(adjustment);
        Move(id, _pending[id].Turn + adjustment);
    }

    public TimelineOccurrence? Advance()
    {
        var next = Preview(1).FirstOrDefault();
        if (next is null) return null;
        _pending.Remove(next.Id);
        CurrentTurn = next.Turn;
        Changed?.Invoke();
        return next;
    }

    public void Clear()
    {
        _pending.Clear();
        Changed?.Invoke();
    }

    internal static void Validate(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }
}


