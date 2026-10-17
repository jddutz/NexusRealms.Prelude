namespace NexusRealms.Prelude.Actions;

/// <summary>Elevates the caster for one timeline turn per Focus remaining after commitment costs.</summary>
public sealed record LevitateAction : AbilityAction
{
    public LevitateAction() : base("ability:Levitate") { }
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("elevated", new(StatusClock.Timeline, 1), ScaleDurationBySourceFocus: true)];
}
