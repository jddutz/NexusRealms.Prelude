namespace NexusRealms.Prelude.Actions;

/// <summary>Immediately grants one Focus to the acting character.</summary>
public sealed record DowseAction : AbilityAction
{
    public DowseAction() : base("ability:Dowse") { }

    public override ImmutableArray<ActionEffect> Execute() =>
    [
        new GrantEffect(new GrantResource(ResourceKind.Focus, 1)) { Recipients = RecipientScope.Source },
    ];
}
