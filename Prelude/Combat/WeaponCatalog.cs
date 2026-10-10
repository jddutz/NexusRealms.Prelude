using System.Collections.ObjectModel;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

public enum WeaponType { Sword, Axe, Knives, Clubs, Maces, Flail, Pistol, Bow, Crossbow, Sling }

/// <summary>Weapon damage, range, and interchangeable icon variants.</summary>
public static class WeaponCatalog
{
    public static ItemData Get(WeaponType type) => WithImpact(type) with
    {
        ThrowDamage = type switch
        {
            WeaponType.Sword => new(new Dictionary<DamageType, int> { [DamageType.Piercing] = 1 }),
            WeaponType.Knives => new(new Dictionary<DamageType, int> { [DamageType.Piercing] = 2 }),
            _ => null,
        },
    };
    private static ItemData WithImpact(WeaponType type)
    {
        var data = GetCore(type);
        return data with { Weapon = data.Weapon! with { ImpactTexture = type switch
        {
            WeaponType.Sword => "effects.slash_01.png",
            WeaponType.Axe => "effects.slash_02.png",
            WeaponType.Knives => "effects.slash_03.png",
            WeaponType.Clubs => "effects.impact_02.png",
            WeaponType.Maces => "effects.impact_03.png",
            WeaponType.Flail => "effects.impact_04.png",
            WeaponType.Pistol => "effects.ranged_01.png",
            WeaponType.Bow => "effects.arrow_01.png",
            WeaponType.Crossbow => "effects.arrow_02.png",
            WeaponType.Sling => "effects.ranged_02.png",
            _ => "effects.impact_01.png",
        } } };
    }
    private static ItemData GetCore(WeaponType type) => type switch
    {
        WeaponType.Sword => Define(
            ["weapons.sword_01.png", "weapons.sword_02.png", "weapons.sword_03.png", "weapons.sword_04.png",
             "weapons.sword_05.png", "weapons.sword_06.png", "weapons.sword_07.png"],
            (DamageType.Slashing, 2)),
        WeaponType.Axe => Define(
            ["weapons.axe_01.png", "weapons.axe_02.png", "weapons.axe_03.png", "weapons.axe_04.png", "weapons.axe_05.png"],
            (DamageType.Slashing, 1), (DamageType.Crushing, 1)),
        WeaponType.Knives => Define(
            ["weapons.knife_01.png", "weapons.knife_02.png", "weapons.knife_03.png", "weapons.knife_04.png", "weapons.knife_05.png"],
            (DamageType.Piercing, 1), (DamageType.Slashing, 1)),
        WeaponType.Clubs => Define(["weapons.club_01.png", "weapons.club_02.png", "weapons.hammer_01.png"], (DamageType.Crushing, 2)),
        WeaponType.Maces => Define(["weapons.spiked_mace_01.png", "weapons.spiked_mace_02.png"],
            (DamageType.Crushing, 1), (DamageType.Piercing, 1)),
        WeaponType.Flail => Define(["weapons.spiked_flail_01.png", "weapons.spiked_flail_02.png"],
            (DamageType.Crushing, 1), (DamageType.Piercing, 1)),
        WeaponType.Pistol => Ranged(["weapons.pistol_01.png", "weapons.pistol_02.png", "weapons.pistol_03.png"], 3, 1.4f, 0.1f, AmmunitionType.Bullet),
        WeaponType.Bow => Ranged(["weapons.bow_01.png", "weapons.bow_02.png", "weapons.bow_03.png"], 3, 0.8f, 0.4f, AmmunitionType.Arrow, twoHanded: true),
        WeaponType.Crossbow => Ranged(["weapons.crossbow_01.png"], 3, 2.1f, 0.2f, AmmunitionType.Bolt, twoHanded: true),
        WeaponType.Sling => Ranged(["weapons.sling_01.png"], 2, 0.8f, 0.6f, AmmunitionType.Stone),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static CarriedItem Create(WeaponType type, ItemId id, string name, string slot, int iconIndex = 0)
    {
        var data = Get(type);
        return new(id, name, slot, data.Icons, data.Weapon)
        {
            IconIndex = iconIndex,
            ThrowDamage = data.ThrowDamage,
            Ammunition = data.Ammunition,
        };
    }

    public static CarriedItem CreateAmmunition(AmmunitionType type, ItemId id, int quantity = 1) =>
        new(id, type.ToString(), "Inventory", ["ui.item_placeholder_acc_pouch.png"])
        {
            Ammunition = type, Quantity = quantity,
        };

    private static ItemData Ranged(string[] icons, int damage, float reload, float attack, AmmunitionType ammunition, bool twoHanded = false) => new()
    {
        Icons = icons,
        Weapon = new WeaponData
        {
            Ammunition = ammunition, TwoHanded = twoHanded, IsMelee = false, Range = WeaponRange.Long, Shots = 1, ReloadTime = reload, AttackSpeed = attack,
            Damage = new ReadOnlyDictionary<DamageType, int>(new Dictionary<DamageType, int> { [DamageType.Piercing] = damage }),
        },
    };

    private static ItemData Define(string[] icons, params (DamageType Type, int Amount)[] points) => new()
    {
        Icons = icons,
        Weapon = new WeaponData
        {
            Damage = new ReadOnlyDictionary<DamageType, int>(points.Append((DamageType.General, 1)).ToDictionary(point => point.Item1, point => point.Item2)),
        },
    };
}
