using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Attacks every selected front-row opponent with each equipped melee weapon.</summary>
public sealed record RampageAction : AbilityAction
{
    /// <summary>Creates the front-row melee attack.</summary>
    public RampageAction() : base("ability:Rampage")
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
