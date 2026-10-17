namespace NexusRealms.Prelude.Actions;

/// <summary>Deals two Basic and one Electrical damage to every selected enemy.</summary>
public sealed record HurricaneAction : AbilityAction
{
    /// <summary>Creates the runtime implementation bound to Hurricane's authored data.</summary>
    public HurricaneAction() : base("ability:Hurricane") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
    [
        new DamageEffect(DamageType.Basic, 2),
        new DamageEffect(DamageType.Lightning, 1),
    ];
}
