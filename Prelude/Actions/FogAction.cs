namespace NexusRealms.Prelude.Actions;

/// <summary>Blinds all selected enemies for two timeline turns.</summary>
public sealed record FogAction : AbilityAction
{
    public FogAction() : base("ability:Fog") { }
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("blind", new(StatusClock.Timeline, 2))];
}
