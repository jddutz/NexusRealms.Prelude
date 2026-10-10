using System.Collections.ObjectModel;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

/// <summary>A snapshot of discrete typed damage points in one hit.</summary>
public sealed class DamageProfile
{
    public IReadOnlyDictionary<DamageType, int> Typed { get; }
    public int Total { get; }

    public DamageProfile(IReadOnlyDictionary<DamageType, int>? typed = null)
    {
        var points = new Dictionary<DamageType, int>();
        foreach (var (type, amount) in typed ?? new Dictionary<DamageType, int>())
        {
            if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(typed));
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            if (amount > 0) points.Add(type, amount);
        }
        Typed = new ReadOnlyDictionary<DamageType, int>(points);
        Total = points.Values.Sum();
    }

    /// <summary>Counts points in a category; General has no category.</summary>
    public int AmountIn(DamageCategory category)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        return Typed.Where(point => point.Key.GetCategory() == category).Sum(point => point.Value);
    }

    public static DamageProfile ForStrike(WeaponData? weapon)
    {
        var bonus = new Dictionary<DamageType, int>(weapon?.Damage ?? new Dictionary<DamageType, int> { [DamageType.General] = 1 });
        if (weapon is { Sharp: true } && bonus.GetValueOrDefault(DamageType.Slashing) > 0)
            bonus[DamageType.Slashing] = checked(bonus[DamageType.Slashing] + 1);
        return new(bonus);
    }

    /// <summary>Removes every point of the specified type, including General.</summary>
    public DamageProfile Negate(DamageType type)
    {
        var remaining = new Dictionary<DamageType, int>(Typed);
        remaining.Remove(type);
        return new(remaining);
    }

    /// <summary>Removes up to the specified number of matching points.</summary>
    public DamageProfile Reduce(DamageType type, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var remaining = new Dictionary<DamageType, int>(Typed);
        remaining[type] = Math.Max(0, remaining.GetValueOrDefault(type) - amount);
        return new(remaining);
    }
}
