using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies one-turn, one-point resistance to Fire, Ice, and Lightning damage per incoming attack.</summary>
public sealed record ElementalWardAction : AbilityAction
{
    /// <summary>Creates the Elemental Ward capability.</summary>
    public ElementalWardAction() : base("ability:ElementalWard") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.ElementalWard.Id, StatusEffects.ElementalWard.Persistence)];
}
