using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Strikes one opponent with the rightmost available melee weapon, then pushes them back.</summary>
public sealed record ChargeAction : AbilityAction
{
    /// <summary>Creates the melee charge action.</summary>
    public ChargeAction() : base("ability:Charge")
    {
        Classifications = ["weapon", "melee"];
    }

    /// <inheritdoc/>
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout) =>
        MeleeWeaponAttacks.Primary(loadout)?.Weapon?.Range ?? WeaponRange.Short;

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [.. MeleeWeaponAttacks.Single(null), new ShiftRowEffect(RowShiftDirection.Backward)];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var weapon = MeleeWeaponAttacks.Primary(runtime.Characters[sourceId].Loadout);
        return [.. MeleeWeaponAttacks.Single(weapon), new ShiftRowEffect(RowShiftDirection.Backward)];
    }
}
