using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Improves Physical damage resistance while the owner has an equipped shield.</summary>
public sealed record ShieldMasteryAction() : AbilityAction("ability:ShieldMastery")
{
    private static readonly ImmutableArray<ActionEffect> ShieldRules =
        [new ResistEffect(new(Category: DamageCategory.Physical), 1, ShareAcrossTypes: true)];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> PassiveRules => ShieldRules;

    /// <inheritdoc/>
    public override bool CanApplyPassiveRules(CombatLoadout loadout) =>
        loadout.Equipment.Values.Any(id =>
            loadout.Items.Any(item => item.Id.Value == id && item.Quantity > 0
                && item.EquipmentKinds.Contains(EquipmentKind.Shield)));
}
