using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies one-turn, one-point resistance to Lightning damage per incoming attack.</summary>
public sealed record StormWardAction : AbilityAction
{
    /// <summary>Creates the Storm Ward capability.</summary>
    public StormWardAction() : base("ability:StormWard") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.StormWard.Id, StatusEffects.StormWard.Persistence)];
}
