namespace NexusRealms.Prelude.Actions;

/// <summary>Prevents the selected enemy from spending Focus for two timeline turns.</summary>
public sealed record BewilderAction : AbilityAction
{
    public BewilderAction() : base("ability:Bewilder") { }
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("bewildered", new(StatusClock.Timeline, 2))];
}
