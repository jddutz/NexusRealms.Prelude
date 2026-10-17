using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Consumes all remaining Focus and strikes once with a Focus-scaled Basic damage bonus.</summary>
public sealed record KingMakerAction : AbilityAction
{
    /// <summary>Creates the King Maker action.</summary>
    public KingMakerAction() : base("ability:KingMaker")
    {
        Classifications = ["weapon", "melee"];
    }

    /// <inheritdoc/>
    public override WeaponRange? GetMeleeRange(CombatLoadout loadout) =>
        MeleeWeaponAttacks.Primary(loadout)?.Weapon?.Range ?? WeaponRange.Short;

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var loadout = runtime.Characters[sourceId].Loadout;
        var focus = loadout.Focus;
        return
        [
            new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), focus)
            {
                Recipients = RecipientScope.Source,
            },
            .. MeleeWeaponAttacks.Single(MeleeWeaponAttacks.Primary(loadout)),
            new DamageEffect(DamageType.Basic, BonusDamage(focus))
            {
                Id = "king-maker-bonus",
            },
        ];
    }

    /// <summary>Gets the bonus for the consumed Focus count, extending the authored values by recurrence.</summary>
    /// <param name="focus">The nonnegative amount of Focus to consume.</param>
    /// <returns>The bonus damage, saturated at the maximum supported integer amount.</returns>
    private static int BonusDamage(int focus)
    {
        if (focus < 0) throw new ArgumentOutOfRangeException(nameof(focus));
        ReadOnlySpan<int> authored = [1, 2, 3, 5, 8, 13, 19, 26, 34];
        if (focus < authored.Length) return authored[focus];

        long previous = 26;
        long current = 34;
        for (var index = authored.Length; index <= focus; index++)
        {
            var next = Math.Min((long)int.MaxValue, previous + current);
            previous = current;
            current = next;
            if (current == int.MaxValue) return int.MaxValue;
        }
        return (int)current;
    }
}
