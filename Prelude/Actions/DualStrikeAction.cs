using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Attacks one opponent once with each equipped melee weapon.</summary>
public sealed record DualStrikeAction : AbilityAction
{
    /// <summary>Creates the dual-strike action.</summary>
    public DualStrikeAction() : base("ability:DualStrike")
    {
        Classifications = ["weapon", "melee"];
    }

    /// <inheritdoc/>
    public override bool CanUse(CombatLoadout loadout) => MeleeWeaponAttacks.Weapons(loadout).Length > 0;

    /// <inheritdoc/>
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout) =>
        MeleeWeaponAttacks.Weapons(loadout) is { Length: > 0 } weapons
            ? weapons.Max(item => item.Weapon!.Range)
            : null;

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId) =>
        MeleeWeaponAttacks.Each(runtime.Characters[sourceId].Loadout);
}
