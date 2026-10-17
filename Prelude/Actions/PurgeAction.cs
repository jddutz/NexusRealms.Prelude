namespace NexusRealms.Prelude.Actions;

/// <summary>Removes every positive and negative status from the selected target.</summary>
public sealed record PurgeAction : AbilityAction
{
    public PurgeAction() : base("ability:Purge") { }

    public override ImmutableArray<ActionEffect> Execute() => [new DispelEffect(new())];
}
