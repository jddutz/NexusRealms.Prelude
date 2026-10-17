using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies Fear to one opponent within short melee reach.</summary>
public sealed record BluffAction : AbilityAction
{
    /// <summary>Creates the short-range melee action.</summary>
    public BluffAction() : base("ability:Bluff")
    {
        MeleeRange = WeaponRange.Short;
    }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Fear.Id, StatusEffects.Fear.Persistence)];
}
