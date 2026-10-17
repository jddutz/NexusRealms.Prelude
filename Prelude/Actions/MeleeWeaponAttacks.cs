using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Actions;

/// <summary>Builds typed attack effects from currently equipped melee weapons.</summary>
internal static class MeleeWeaponAttacks
{
    /// <summary>Returns each distinct equipped melee weapon in hand order.</summary>
    /// <param name="loadout">The loadout supplying current hand equipment.</param>
    /// <returns>Equipped melee weapons with positive quantity.</returns>
    internal static ImmutableArray<CarriedItem> Weapons(CombatLoadout loadout) =>
        Enum.GetValues<StrikeHand>()
            .Select(loadout.HeldItem)
            .OfType<CarriedItem>()
            .Where(item => item.Quantity > 0 && item.Weapon is { IsMelee: true })
            .DistinctBy(item => item.Id)
            .ToImmutableArray();

    /// <summary>Returns the right-hand melee weapon when available, otherwise the left-hand weapon.</summary>
    /// <param name="loadout">The loadout supplying current hand equipment.</param>
    /// <returns>The preferred equipped melee weapon, or null when neither hand has one.</returns>
    internal static CarriedItem? Primary(CombatLoadout loadout) =>
        IsMeleeWeapon(loadout.HeldItem(StrikeHand.Right))
            ? loadout.HeldItem(StrikeHand.Right)
            : IsMeleeWeapon(loadout.HeldItem(StrikeHand.Left))
                ? loadout.HeldItem(StrikeHand.Left)
                : null;

    /// <summary>Creates one strike with each currently equipped melee weapon.</summary>
    /// <param name="loadout">The loadout supplying current hand equipment.</param>
    /// <returns>Ordered typed damage effects, one attack index per weapon.</returns>
    internal static ImmutableArray<ActionEffect> Each(CombatLoadout loadout)
    {
        var weapons = Weapons(loadout);
        var effects = ImmutableArray.CreateBuilder<ActionEffect>();
        for (var attack = 0; attack < weapons.Length; attack++)
            AddStrike(effects, weapons[attack].Weapon!, attack, weapons.Length > 1);
        return effects.ToImmutable();
    }

    /// <summary>Creates one strike with an equipped weapon, or an unarmed Basic strike when absent.</summary>
    /// <param name="item">The melee weapon to use, or null for an unarmed attack.</param>
    /// <returns>The typed damage effects for the strike.</returns>
    internal static ImmutableArray<ActionEffect> Single(CarriedItem? item)
    {
        var effects = ImmutableArray.CreateBuilder<ActionEffect>();
        AddStrike(effects, item?.Weapon, 0, false);
        return effects.ToImmutable();
    }

    /// <summary>Adds ordered typed damage operations for one attack.</summary>
    /// <param name="effects">The action's effect builder.</param>
    /// <param name="weapon">The weapon supplying damage and reach, or null for an unarmed strike.</param>
    /// <param name="attack">The attack index used for per-attack mitigation.</param>
    /// <param name="omitBasic">Whether to omit the Basic point for a multi-weapon action.</param>
    private static void AddStrike(ImmutableArray<ActionEffect>.Builder effects, WeaponData? weapon, int attack, bool omitBasic)
    {
        var damage = DamageProfile.ForStrike(weapon).Typed.ToDictionary();
        if (omitBasic) damage.Remove(DamageType.Basic);
        foreach (var (type, amount) in damage.OrderBy(pair => pair.Key))
            if (amount > 0)
                effects.Add(new DamageEffect(type, amount)
                {
                    AttackIndex = attack,
                    MeleeRange = weapon?.Range ?? WeaponRange.Short,
                    Id = $"weapon-{attack}:{type}",
                });
    }

    /// <summary>Checks that an item is a live melee weapon.</summary>
    /// <param name="item">The candidate equipped item.</param>
    /// <returns>True when the item has positive quantity and melee weapon data.</returns>
    private static bool IsMeleeWeapon(CarriedItem? item) =>
        item is { Quantity: > 0, Weapon: { IsMelee: true } };
}
