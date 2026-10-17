namespace NexusRealms.Prelude.Actions;

/// <summary>Deals Slashing damage and independently blinds all selected enemies.</summary>
public sealed record SandstormAction : AbilityAction
{
    public SandstormAction() : base("ability:Sandstorm") { }
    public override ImmutableArray<ActionEffect> Execute() =>
    [
        new DamageEffect(DamageType.Slashing, 1),
        new ApplyEffect("blind", new(StatusClock.Timeline, 2)),
    ];
}
