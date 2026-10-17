using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>One attack per equipped melee weapon against the front row.</summary>
public sealed record WhirlwindAction : AbilityAction
{
    /// <summary>Creates the front-row melee attack.</summary>
    public WhirlwindAction() : base("ability:Whirlwind") { Classifications = ["weapon", "melee"]; Flags = ActionFlags.Melee; }

    /// <inheritdoc/>
    public override bool CanUse(CombatLoadout loadout) => MeleeWeaponAttacks.Weapons(loadout).Length > 0;

    /// <inheritdoc/>
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout)
        => MeleeWeaponAttacks.Weapons(loadout) is { Length: > 0 } weapons ? weapons.Max(item => item.Weapon!.Range) : null;

    /// <inheritdoc/>
    public override ImmutableArray<string> SelectTargets(CombatActionResolver runtime, string sourceId, string? targetId)
        => base.SelectTargets(runtime, sourceId, targetId).Where(id => runtime.Characters[id].Position
            is FormationSlot.FrontLeft or FormationSlot.FrontCenter or FormationSlot.FrontRight).ToImmutableArray();

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
        => MeleeWeaponAttacks.Each(runtime.Characters[sourceId].Loadout);
}
