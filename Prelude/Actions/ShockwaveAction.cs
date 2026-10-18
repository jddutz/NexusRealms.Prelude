using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Strikes one opponent with a melee weapon and damages row-adjacent opponents.</summary>
public sealed record ShockwaveAction : AbilityAction
{
    /// <summary>Creates the Shockwave action.</summary>
    public ShockwaveAction() : base("ability:Shockwave")
    {
        Classifications = ["weapon", "melee"];
        Flags = ActionFlags.Melee;
    }

    /// <inheritdoc/>
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout) =>
        MeleeWeaponAttacks.Primary(loadout)?.Weapon?.Range ?? WeaponRange.Short;

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var loadout = runtime.Characters[sourceId].Loadout;
        return
        [
            .. MeleeWeaponAttacks.Single(MeleeWeaponAttacks.Primary(loadout)),
            new DamageEffect(DamageType.Crushing, 1)
            {
                AttackIndex = 1,
                RecipientTargeting = new(TargetShape.AdjacentInRow),
            },
        ];
    }
}
