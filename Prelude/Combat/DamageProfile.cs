using System.Collections.ObjectModel;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

/// <summary>A snapshot of discrete untyped and typed damage points in one hit.</summary>
public sealed class DamageProfile
{
    public int Untyped { get; }
    public IReadOnlyDictionary<DamageType, int> Typed { get; }
    public int Total { get; }

    public DamageProfile(int untyped, IReadOnlyDictionary<DamageType, int>? typed = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(untyped);
        var points = new Dictionary<DamageType, int>();
        foreach (var (type, amount) in typed ?? new Dictionary<DamageType, int>())
        {
            if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(typed));
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            if (amount > 0) points.Add(type, amount);
        }
        Untyped = untyped;
        Typed = new ReadOnlyDictionary<DamageType, int>(points);
        Total = checked(untyped + points.Values.Sum());
    }

    /// <summary>Counts typed points in a category; untyped damage has no category.</summary>
    public int AmountIn(DamageCategory category)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        return Typed.Where(point => point.Key.GetCategory() == category).Sum(point => point.Value);
    }

    public static DamageProfile ForStrike(WeaponData? weapon)
    {
        var bonus = new Dictionary<DamageType, int>(weapon?.BonusDamage ?? new Dictionary<DamageType, int>());
        if (weapon is { Sharp: true } && bonus.GetValueOrDefault(DamageType.Slashing) > 0)
            bonus[DamageType.Slashing] = checked(bonus[DamageType.Slashing] + 1);
        return new(1, bonus);
    }

    /// <summary>Removes every point of a type, preserving untyped damage.</summary>
    public DamageProfile Negate(DamageType type)
    {
        var remaining = new Dictionary<DamageType, int>(Typed);
        remaining.Remove(type);
        return new(Untyped, remaining);
    }

    /// <summary>Removes up to the specified number of matching points.</summary>
    public DamageProfile Reduce(DamageType type, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var remaining = new Dictionary<DamageType, int>(Typed);
        remaining[type] = Math.Max(0, remaining.GetValueOrDefault(type) - amount);
        return new(Untyped, remaining);
    }
}
