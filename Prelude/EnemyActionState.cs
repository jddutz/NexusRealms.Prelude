using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude;

/// <summary>Per-encounter inventory and action selection, using shared player weapon definitions.</summary>
public sealed class EnemyActionState(CharacterData definition)
{
    private readonly List<CarriedItem> _items = definition.Items.ToList();
    private readonly Dictionary<ItemId, int> _shots = [];
    public IReadOnlyList<CarriedItem> Items => _items;
    public int ShotsRemaining(CarriedItem item) => _shots.GetValueOrDefault(item.Id, item.Weapon?.Shots ?? 0);

    public EnemyAttackData Select(Random random)
    {
        var choices = definition.Attacks.Where(action => action.ItemId is null
            || _items.Any(item => item.Id == action.ItemId && item.Quantity > 0)).ToArray();
        if (choices.Length == 0) return new() { Animation = "Idle", Damage = new DamageProfile() };
        var action = choices[random.Next(choices.Length)];
        if (action.ItemId is not { } id) return action;
        var item = _items.Single(item => item.Id == id);
        if (action.Throw)
            return action with { TurnCost = CombatLoadout.Throw.TurnCost, Damage = item.ThrowDamage ?? DamageProfile.ForStrike(item.Weapon) };
        if (item.Weapon is not { } weapon) throw new InvalidOperationException("Enemy attack item is not a weapon.");
        if (weapon.Shots > 0 && ShotsRemaining(item) == 0)
        {
            if (!_items.Any(ammo => ammo.Ammunition == weapon.Ammunition && ammo.Quantity > 0))
                return new() { Animation = "Idle", Damage = new DamageProfile() };
            return action with { Reload = true, ImpactFraction = 1f, Animation = action.ReloadAnimation, TurnCost = weapon.ReloadTime, Damage = new DamageProfile() };
        }
        return action with { TurnCost = weapon.AttackSpeed, Damage = DamageProfile.ForStrike(weapon) };
    }

    /// <summary>Commits ammunition or thrown inventory once, at the action's impact cue.</summary>
    public void Apply(EnemyAttackData action)
    {
        if (action.ItemId is not { } id) return;
        var item = _items.Single(item => item.Id == id);
        if (action.Throw) { Consume(item); return; }
        if (item.Weapon is not { Shots: > 0 } weapon) return;
        if (action.Reload)
        {
            var ammo = _items.First(ammo => ammo.Ammunition == weapon.Ammunition && ammo.Quantity > 0);
            Consume(ammo);
            _shots[id] = weapon.Shots;
        }
        else
        {
            if (ShotsRemaining(item) == 0) throw new InvalidOperationException("Enemy weapon is empty.");
            _shots[id] = ShotsRemaining(item) - 1;
        }
    }

    private void Consume(CarriedItem item)
    {
        if (item.Quantity <= 1) _items.Remove(item);
        else _items[_items.IndexOf(item)] = item with { Quantity = item.Quantity - 1 };
    }
}
