namespace NexusRealms.Prelude.DataModel;

/// <summary>An authored character capability. Derive a record class for each named action.</summary>
public record ActionData
{
    /// <summary>Stable action identity, independent of an ability identifier.</summary>
    public string Id { get; init; } = "attack";
    /// <summary>Explicit filter classifications such as weapon or spell.</summary>
    public System.Collections.Immutable.ImmutableArray<string> Classifications { get; init; } = [];
    /// <summary>Progression and equipment conditions required to unlock this capability.</summary>
    public ActionRequirements Requirements { get; init; } = new();
    /// <summary>Costs paid in full at commitment.</summary>
    public System.Collections.Immutable.ImmutableArray<ActionCost> Costs { get; init; } = [];
    /// <summary>Ordered typed operations; null adapts the existing typed damage profile.</summary>
    public System.Collections.Immutable.ImmutableArray<ActionEffect>? Effects { get; init; }
    /// <summary>Target selection evaluated once when resolution begins.</summary>
    public TargetingProfile Targeting { get; init; } = new();
    /// <summary>Returns authored effects or a compatibility projection of the legacy attack.</summary>
    public virtual System.Collections.Immutable.ImmutableArray<ActionEffect> BuildEffects() => Effects
        ?? Damage.Typed.Select(pair => (ActionEffect)new DamageEffect(pair.Key, pair.Value)).ToImmutableArray();

    /// <summary>Optional weapon used by legacy attack playback.</summary>
    public ItemId? ItemId { get; init; }
    /// <summary>Whether legacy playback releases the owned item.</summary>
    public bool Throw { get; init; }
    /// <summary>Whether legacy playback reloads the weapon.</summary>
    public bool Reload { get; init; }
    /// <summary>Presentation animation for execution.</summary>
    public string Animation { get; init; } = "Attack";
    /// <summary>Presentation animation for reloading.</summary>
    public string ReloadAnimation { get; init; } = "Idle";
    /// <summary>Positive cost in combat timeline units.</summary>
    public float TurnCost { get; init; } = 1f;
    /// <summary>Compatibility damage for existing weapon attacks.</summary>
    public DamageProfile Damage { get; init; } =
        new(new Dictionary<DamageType, int> { [DamageType.General] = 1 });
    /// <summary>Presentation cue for impact within the animation.</summary>
    public float ImpactFraction { get; init; } = 0.6f;
}
