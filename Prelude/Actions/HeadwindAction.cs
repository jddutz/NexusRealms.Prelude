namespace NexusRealms.Prelude.Actions;

/// <summary>Slows the selected enemy for three timeline turns.</summary>
public sealed record HeadwindAction : AbilityAction
{
    /// <summary>Creates the runtime implementation bound to Headwind's authored data.</summary>
    public HeadwindAction() : base("ability:Headwind") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("slow", new(StatusClock.Timeline, 3))];
}
