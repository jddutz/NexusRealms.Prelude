namespace NexusRealms.Prelude.Actions;

public sealed record PressureShieldAction : AbilityAction
{
    public PressureShieldAction() : base("ability:PressureShield") { }
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect("pressure-shield", new(StatusClock.Timeline, 1), ScaleDurationBySourceFocus: true)];
}
