namespace NexusRealms.Prelude.Actions;

/// <summary>Restores three Health without a gameplay maximum.</summary>
public sealed record HealAction : AbilityAction
{
    public HealAction() : base("ability:Heal") { }

    public override ImmutableArray<ActionEffect> Execute() => [new RestoreEffect(3)];
}
