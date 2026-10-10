namespace NexusRealms.Prelude.Combat;

/// <summary>Combat health and damage notifications, independent of character artwork.</summary>
public sealed class DamageReceiver
{
    private int _health;
    public DamageReceiver(int health) => Health = health;
    public int Health
    {
        get => _health;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_health == value) return;
            _health = value;
            HealthChanged?.Invoke();
        }
    }
    public event Action? HealthChanged;
    public event Action<DamageTaken>? DamageTaken;
    public event Action<AttackImpact>? ImpactOccurred;
    internal void NotifyImpact(AttackImpact impact) => ImpactOccurred?.Invoke(impact);
    internal void NotifyDamage(DamageTaken result) => DamageTaken?.Invoke(result);
}

/// <summary>Resolved typed damage and the resulting health change. Overkill retains its damage types.</summary>
public sealed record DamageTaken(DamageProfile Damage, int HealthBefore, int HealthAfter)
{
    public int HealthLost => HealthBefore - HealthAfter;
    public bool IsLethal => HealthBefore > 0 && HealthAfter == 0;
}

/// <summary>Shared damage application for actions, ongoing effects, and environmental hazards.</summary>
public static class DamageResolver
{
    /// <summary>Applies damage after optional mitigation, then notifies presentation listeners.</summary>
    public static DamageTaken Apply(DamageReceiver target, DamageProfile damage,
        Func<DamageProfile, DamageProfile>? mitigate = null, AttackImpact? impact = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(damage);
        var before = target.Health;
        var resolved = before == 0 ? new DamageProfile()
            : mitigate is null ? damage
            : mitigate(damage) ?? throw new InvalidOperationException("Mitigation must return a damage profile.");
        var after = Math.Max(0, before - resolved.Total);
        var result = new DamageTaken(resolved, before, after);
        target.Health = after;
        if (before > 0 && impact is not null)
            target.NotifyImpact(result.HealthLost == 0 ? AttackImpact.Blocked : impact);
        if (result.HealthLost > 0) target.NotifyDamage(result);
        return result;
    }
}
