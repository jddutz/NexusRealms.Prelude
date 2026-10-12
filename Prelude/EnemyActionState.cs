using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude;

/// <summary>Legacy weapon playback adapter; selection and AI use the same preparation rules.</summary>
public sealed class EnemyActionState
{
    public EnemyActionState(CharacterData definition, CombatLoadout? loadout = null) => Loadout = loadout ?? CombatLoadout.FromCharacter(definition);
    public EnemyActionState(CombatLoadout loadout) => Loadout = loadout;
    public CombatLoadout Loadout { get; }
    public IReadOnlyList<CarriedItem> Items => Loadout.Items;
    public int ShotsRemaining(CarriedItem item) => Loadout.ShotsRemaining(item);
    public ActionData? Prepare(ActionData action)
    {
        if (!Loadout.MeetsRequirements(action)) return null;
        if (action.ItemId is not { } id) return action;
        var item = Loadout.Items.Find(item => item.Id == id && item.Quantity > 0);
        if (item is null) return null;
        if (action.Throw)
        {
            if (!Loadout.ThrowOptions().Any(option => option.Item.Id == id)) return null;
            return action with { TurnCost = CombatLoadout.Throw.TurnCost, Damage = item.ThrowDamage ?? DamageProfile.ForStrike(item.Weapon) };
        }
        if (item.Weapon is not { } weapon) throw new InvalidOperationException("Enemy attack item is not a weapon.");
        var hand = Enum.GetValues<StrikeHand>().FirstOrDefault(hand => Loadout.HeldItem(hand)?.Id == id);
        if (Loadout.HeldItem(hand)?.Id != id || !Loadout.HasShootingHands(hand)) return null;
        if (weapon.Shots > 0 && ShotsRemaining(item) == 0)
        {
            if (!Loadout.Items.Any(ammo => ammo.Ammunition == weapon.Ammunition && ammo.Quantity > 0)) return null;
            return action with { Reload = true, ImpactFraction = 1f, Animation = action.ReloadAnimation,
                TurnCost = weapon.ReloadTime, Damage = new DamageProfile() };
        }
        return action with { TurnCost = weapon.AttackSpeed, Damage = DamageProfile.ForStrike(weapon) };
    }
    public ActionData Select(Random random, Func<ActionData, bool>? allowed = null)
    {
        var choices = Loadout.Actions.Values.Where(action => allowed?.Invoke(action) ?? true)
            .Select(Prepare).OfType<ActionData>().OrderBy(action => action.Id, StringComparer.Ordinal).ToArray();
        return choices.Length == 0 ? new() { Animation = "Idle", Damage = new DamageProfile() } : choices[random.Next(choices.Length)];
    }
    public void Apply(ActionData action)
    {
        if (action.ItemId is not { } id) return;
        var item = Loadout.Items.Single(item => item.Id == id);
        if (action.Throw) { Loadout.ConsumeItem(id, 1); return; }
        if (item.Weapon is not { Shots: > 0 } weapon) return;
        if (action.Reload)
        {
            var ammo = Loadout.Items.OrderBy(item => item.Id.Value, StringComparer.Ordinal)
                .First(ammo => ammo.Ammunition == weapon.Ammunition && ammo.Quantity > 0);
            Loadout.ConsumeItem(ammo.Id, 1);
            Loadout.SetWeaponShots(id, weapon.Shots);
        }
        else
        {
            if (ShotsRemaining(item) == 0) throw new InvalidOperationException("Enemy weapon is empty.");
            Loadout.SetWeaponShots(id, ShotsRemaining(item) - 1);
        }
    }
}
