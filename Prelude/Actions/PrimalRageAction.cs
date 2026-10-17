namespace NexusRealms.Prelude.Actions;

/// <summary>Grants its owner one Focus per point of Basic Health damage taken.</summary>
public sealed record PrimalRageAction() : AbilityAction("ability:PrimalRage")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> OnDamageTaken(DamageType damageType, int actualDamage) =>
        damageType == DamageType.Basic && actualDamage > 0
            ? [new GrantEffect(new GrantResource(ResourceKind.Focus, actualDamage)) { Recipients = RecipientScope.Source }]
            : [];
}
