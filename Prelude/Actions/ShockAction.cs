namespace NexusRealms.Prelude.Actions;

/// <summary>Removes up to three Focus from the selected enemy.</summary>
public sealed record ShockAction() : AbilityAction("ability:Shock")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 3)];
}
