using System.Collections.ObjectModel;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

public enum WeaponType { Sword, Axe, Knives, Clubs, Maces, Flail, Pistol }

/// <summary>Weapon damage, range, and interchangeable icon variants.</summary>
public static class WeaponCatalog
{
    public static ItemData Get(WeaponType type) => GetCore(type) with
    {
        ThrowDamage = type switch
        {
            WeaponType.Sword => new(new Dictionary<DamageType, int> { [DamageType.Piercing] = 1 }),
            WeaponType.Knives => new(new Dictionary<DamageType, int> { [DamageType.Piercing] = 2 }),
            _ => null,
        },
    };
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
        WeaponType.Pistol => new()
        {
            Weapon = new WeaponData
            {
                Range = WeaponRange.Long,
                IsMelee = false,
                Damage = new ReadOnlyDictionary<DamageType, int>(new Dictionary<DamageType, int>
                {
                    [DamageType.Piercing] = 3,
                }),
            },
        },
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static CarriedItem Create(WeaponType type, ItemId id, string name, string slot, int iconIndex = 0)
    {
        var data = Get(type);
        return new(id, name, slot, data.Icons, data.Weapon)
        {
            IconIndex = iconIndex,
            ThrowDamage = data.ThrowDamage,
        };
    }

    private static ItemData Define(string[] icons, params (DamageType Type, int Amount)[] points) => new()
    {
        Icons = icons,
        Weapon = new WeaponData
        {
            Damage = new ReadOnlyDictionary<DamageType, int>(points.Append((DamageType.General, 1)).ToDictionary(point => point.Item1, point => point.Item2)),
        },
    };
}
