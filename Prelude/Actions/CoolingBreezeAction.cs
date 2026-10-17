namespace NexusRealms.Prelude.Actions;

/// <summary>Protects the selected recipient against Fire damage for three timeline turns.</summary>
public sealed record CoolingBreezeAction : AbilityAction
{
    public CoolingBreezeAction() : base("ability:CoolingBreeze") { }

    public override ImmutableArray<ActionEffect> Execute() =>
    [
        new ApplyEffect("cooling-breeze", new(StatusClock.Timeline, 3)),
    ];
}
