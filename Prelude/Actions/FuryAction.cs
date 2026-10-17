using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Strikes one enemy with each equipped melee weapon and gains a stackable speed boost afterward.</summary>
public sealed record FuryAction : AbilityAction
{
    /// <summary>Creates the Fury action.</summary>
    public FuryAction() : base("ability:Fury")
    {
        Classifications = ["weapon", "melee"];
    }

    /// <inheritdoc/>
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout) =>
        MeleeWeaponAttacks.Weapons(loadout) is { Length: > 0 } weapons
            ? weapons.Max(item => item.Weapon!.Range)
            : WeaponRange.Short;

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var attacks = MeleeWeaponAttacks.Each(runtime.Characters[sourceId].Loadout);
        return
        [
            .. attacks,
            new ApplyEffect(StatusEffects.Fury.Id, StatusEffects.Fury.Persistence)
            {
                Recipients = RecipientScope.Source,
            },
        ];
    }
}
