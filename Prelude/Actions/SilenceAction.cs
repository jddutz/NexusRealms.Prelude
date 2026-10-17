namespace NexusRealms.Prelude.Actions;

/// <summary>Prevents the selected enemy from using actions with magical affinity for two timeline turns.</summary>
public sealed record SilenceAction : AbilityAction
{
    /// <summary>Creates the implementation bound to Silence's authored data.</summary>
    public SilenceAction() : base("ability:Silence") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("silenced", new(StatusClock.Timeline, 2))];
}
