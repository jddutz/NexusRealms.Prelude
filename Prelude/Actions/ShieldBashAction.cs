namespace NexusRealms.Prelude.Actions;

/// <summary>Bashes one opponent with an equipped shield, damaging and disrupting them.</summary>
public sealed record ShieldBashAction : AbilityAction
{
    /// <summary>Creates the shield-gated melee action.</summary>
    public ShieldBashAction() : base("ability:ShieldBash")
    {
        Classifications = ["melee"];
        Flags = ActionFlags.Melee;
        MeleeRange = WeaponRange.Short;
        Requirements = new()
        {
            Equipment = [new(AnyOf: [EquipmentKind.Shield])],
        };
    }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
    [
        new DamageEffect(DamageType.Basic, 2),
        new DelayEffect(0.8f),
        new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 1),
    ];
}
