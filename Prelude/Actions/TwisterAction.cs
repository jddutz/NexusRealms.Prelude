namespace NexusRealms.Prelude.Actions;

/// <summary>Deals Crushing damage and delays every selected enemy's next turn.</summary>
public sealed record TwisterAction : AbilityAction
{
    /// <summary>Creates the implementation bound to Twister's authored data.</summary>
    public TwisterAction() : base("ability:Twister") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
    [
        new DamageEffect(DamageType.Crushing, 1),
        new DelayEffect(0.5f),
    ];
}
