using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>One attack per equipped melee weapon against the front row.</summary>
public sealed record WhirlwindAction : AbilityAction
{
    public WhirlwindAction() : base("ability:Whirlwind") { Classifications = ["weapon", "melee"]; }
    private static CarriedItem[] Weapons(CombatLoadout loadout) => Enum.GetValues<StrikeHand>()
        .Select(loadout.HeldItem).OfType<CarriedItem>().Where(item => item.Weapon is { IsMelee: true } && item.Quantity > 0)
        .DistinctBy(item => item.Id).ToArray();
    public override bool CanUse(CombatLoadout loadout) => Weapons(loadout).Length > 0;
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout)
        => Weapons(loadout) is { Length: > 0 } weapons ? weapons.Max(item => item.Weapon!.Range) : null;
    public override ImmutableArray<string> SelectTargets(CombatActionResolver runtime, string sourceId, string? targetId)
        => base.SelectTargets(runtime, sourceId, targetId).Where(id => runtime.Characters[id].Position
            is FormationSlot.FrontLeft or FormationSlot.FrontCenter or FormationSlot.FrontRight).ToImmutableArray();
    public override ImmutableArray<ActionEffect> Execute() => [];
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var weapons = Weapons(runtime.Characters[sourceId].Loadout);
        var effects = ImmutableArray.CreateBuilder<ActionEffect>();
        for (var attack = 0; attack < weapons.Length; attack++)
        {
            var weapon = weapons[attack].Weapon!;
            var damage = DamageProfile.ForStrike(weapon);
            var amounts = damage.Typed.ToDictionary();
            if (weapons.Length > 1)
            {
                amounts.Remove(DamageType.Basic);
            }
            foreach (var (type, amount) in amounts.OrderBy(pair => pair.Key))
                if (amount > 0) effects.Add(new DamageEffect(type, amount)
                { AttackIndex = attack, MeleeRange = weapon.Range, Id = $"weapon-{attack}:{type}" });
        }
        return effects.ToImmutable();
    }
}
