using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Mutable action state referencing existing health and loadout owners.</summary>
public sealed class ActionCharacterState
{
    /// <summary>Creates a runtime character; the player must not have a formation position.</summary>
    public ActionCharacterState(string id, bool player, CombatTeam team, DamageReceiver health, CombatLoadout loadout, FormationSlot? position = null)
    {
        if (string.IsNullOrWhiteSpace(id) || player && position is not null || position is { } slot && !Enum.IsDefined(slot))
            throw new ArgumentException("Invalid runtime character identity or position.");
        Id = id; IsPlayer = player; Team = team; Health = health; Loadout = loadout; Position = position;
    }
    /// <summary>Scheduler identity.</summary>
    public string Id { get; }
    /// <summary>Whether this is the player with no formation position.</summary>
    public bool IsPlayer { get; }
    /// <summary>Combat relationship used by targeting.</summary>
    public CombatTeam Team { get; }
    /// <summary>The existing health owner.</summary>
    public DamageReceiver Health { get; }
    /// <summary>The existing resource, equipment, and inventory owner.</summary>
    public CombatLoadout Loadout { get; }
    /// <summary>Authoritative non-player formation position.</summary>
    public FormationSlot? Position { get; internal set; }
    /// <summary>Optional explicitly authored Health cap; null preserves current uncapped behavior.</summary>
    public int? MaximumHealth { get; init; }
    /// <summary>Persistent equipment or innate rules; no implicit status-name mechanics exist.</summary>
    public IReadOnlyList<ActionEffect> Rules { get; init; } = [];
    /// <summary>Active runtime statuses in stable application order.</summary>
    public IReadOnlyList<ActiveStatus> Statuses => ActiveStatuses.AsReadOnly();
    /// <summary>Resolver-owned status storage.</summary>
    internal List<ActiveStatus> ActiveStatuses { get; } = [];
    /// <summary>Completed recipient turns used by explicit turn-start expiration.</summary>
    internal int TurnsStarted { get; set; }
}

/// <summary>An independent status instance retaining definition, source, expiration, and uses.</summary>
public sealed class ActiveStatus
{
    /// <summary>Creates a validated instance at a particular clock position.</summary>
    internal ActiveStatus(long sequence, StatusEffectData definition, string source, StatusPersistence persistence, float turn, int turnsStarted)
    {
        Sequence = sequence; Definition = definition; SourceId = source; Persistence = persistence;
        ExpiresAt = persistence.Clock == StatusClock.Timeline ? turn + persistence.Duration : null;
        ExpiresOnTurnStart = persistence.Clock == StatusClock.RecipientTurnStart ? turnsStarted + (int)persistence.Duration : null;
        RemainingUses = persistence.Uses;
    }
    /// <summary>Stable application order and attribution identity.</summary>
    public long Sequence { get; }
    /// <summary>The authored status and its rules.</summary>
    public StatusEffectData Definition { get; }
    /// <summary>Character responsible for applying this status.</summary>
    public string SourceId { get; }
    /// <summary>Explicit expiration and use policy.</summary>
    public StatusPersistence Persistence { get; }
    /// <summary>Exclusive absolute timeline expiration.</summary>
    public float? ExpiresAt { get; }
    /// <summary>Recipient turn-start count on which this instance expires.</summary>
    public int? ExpiresOnTurnStart { get; }
    /// <summary>Remaining qualifying incoming actions, when bounded.</summary>
    public int? RemainingUses { get; internal set; }
}
