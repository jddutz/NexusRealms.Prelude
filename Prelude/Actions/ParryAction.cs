using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Actions;

/// <summary>Uses equipped weapons' typed damage profiles to mitigate the next incoming melee action.</summary>
public sealed record ParryAction() : AbilityAction("ability:Parry")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Parry.Id, StatusEffects.Parry.Persistence)];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var loadout = runtime.Characters[sourceId].Loadout;
        var resistance = EquippedWeapons(loadout)
            .SelectMany(item => DamageProfile.ForStrike(item.Weapon).Typed)
            .GroupBy(point => point.Key)
            .OrderBy(group => group.Key)
            .Select(group => (ActionEffect)new ResistEffect(
                new(Type: group.Key, RequiresActionFlags: ActionFlags.Melee),
                group.Sum(point => point.Value)))
            .ToImmutableArray();

        return
        [
            new ApplyEffect(StatusEffects.Parry.Id, StatusEffects.Parry.Persistence)
            {
                RuleOverrides = resistance,
            },
        ];
    }

    /// <inheritdoc/>
    public override bool CanUse(CombatLoadout loadout) => EquippedWeapons(loadout).Length > 0;

    /// <summary>Returns distinct equipped weapons with positive quantity.</summary>
    /// <param name="loadout">The live equipment and inventory owner.</param>
    /// <returns>Weapon items currently associated with equipment slots.</returns>
    private static ImmutableArray<CarriedItem> EquippedWeapons(CombatLoadout loadout) =>
        loadout.Items
            .Where(item => item.Quantity > 0 && item.Weapon is not null
                && loadout.Equipment.Values.Contains(item.Id.Value, StringComparer.Ordinal))
            .DistinctBy(item => item.Id)
            .ToImmutableArray();
}
