using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude;

/// <summary>Per-encounter inventory and action selection, using shared player weapon definitions.</summary>
public sealed class EnemyActionState
{
    /// <summary>Creates AI decisions over the same loadout used by action effects.</summary>
    public EnemyActionState(CharacterData definition, CombatLoadout? loadout = null)
    {
        Loadout = loadout ?? CombatLoadout.FromCharacter(definition);
    }
    /// <summary>Shared resource and inventory state for AI and action effects.</summary>
    public CombatLoadout Loadout { get; }
    /// <summary>Current owned item records.</summary>
    private List<CarriedItem> _items => Loadout.Items;
    public IReadOnlyList<CarriedItem> Items => _items;

    public int ShotsRemaining(CarriedItem item) =>
        Loadout.ShotsRemaining(item);

    public ActionData Select(Random random, Func<ActionData, bool>? allowed = null)
    {
        var choices = Loadout.Actions.Values
            .Where(action => Loadout.MeetsRequirements(action) && (allowed?.Invoke(action) ?? true) && (
                action.ItemId is null
                || _items.Any(item => item.Id == action.ItemId && item.Quantity > 0))
            )
            .ToArray();
        if (choices.Length == 0)
            return new() { Animation = "Idle", Damage = new DamageProfile() };
        var action = choices[random.Next(choices.Length)];
        if (action.ItemId is not { } id)
            return action;
        var item = _items.Single(item => item.Id == id);
        if (action.Throw)
            return action with
            {
                TurnCost = CombatLoadout.Throw.TurnCost,
                Damage = item.ThrowDamage ?? DamageProfile.ForStrike(item.Weapon),
            };
        if (item.Weapon is not { } weapon)
            throw new InvalidOperationException("Enemy attack item is not a weapon.");
        if (weapon.Shots > 0 && ShotsRemaining(item) == 0)
        {
            if (!_items.Any(ammo => ammo.Ammunition == weapon.Ammunition && ammo.Quantity > 0))
                return new() { Animation = "Idle", Damage = new DamageProfile() };
            return action with
            {
                Reload = true,
                ImpactFraction = 1f,
                Animation = action.ReloadAnimation,
                TurnCost = weapon.ReloadTime,
                Damage = new DamageProfile(),
            };
        }
        return action with
        {
            TurnCost = weapon.AttackSpeed,
            Damage = DamageProfile.ForStrike(weapon),
        };
    }

    /// <summary>Commits ammunition or thrown inventory once, at the action's impact cue.</summary>
    public void Apply(ActionData action)
    {
        if (action.ItemId is not { } id)
            return;
        var item = _items.Single(item => item.Id == id);
        if (action.Throw)
        {
            Consume(item);
            return;
        }
        if (item.Weapon is not { Shots: > 0 } weapon)
            return;
        if (action.Reload)
        {
            var ammo = _items.First(ammo =>
                ammo.Ammunition == weapon.Ammunition && ammo.Quantity > 0
            );
            Consume(ammo);
            Loadout.SetWeaponShots(id, weapon.Shots);
        }
        else
        {
            if (ShotsRemaining(item) == 0)
                throw new InvalidOperationException("Enemy weapon is empty.");
            Loadout.SetWeaponShots(id, ShotsRemaining(item) - 1);
        }
    }

    private void Consume(CarriedItem item)
    {
        Loadout.ConsumeItem(item.Id, 1);
    }
}
