using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Rules;

/// <summary>Shared uncapped healing; ordinary restoration never resurrects.</summary>
public static class HealingRules
{
    public static bool CanRestore(DamageReceiver target) => target.Health > 0;

    /// <summary>Restores available Health and returns the actual increase.</summary>
    public static int Restore(DamageReceiver target, double amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!double.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!CanRestore(target)) return 0;
        var before = target.Health;
        var after = (int)Math.Min(int.MaxValue, before + amount);
        target.Health = Math.Max(before, after);
        return target.Health - before;
    }
}
