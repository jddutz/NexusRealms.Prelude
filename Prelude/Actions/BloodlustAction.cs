namespace NexusRealms.Prelude.Actions;

/// <summary>Grants its owner one Focus whenever an opposing character is defeated.</summary>
public sealed record BloodlustAction() : AbilityAction("ability:Bloodlust")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> OnOpposingCharacterDefeated() =>
        [new GrantEffect(new GrantResource(ResourceKind.Focus, 1)) { Recipients = RecipientScope.Source }];
}
