using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies one-turn, one-point resistance to Fire damage per incoming attack.</summary>
public sealed record FlameWardAction : AbilityAction
{
    /// <summary>Creates the Flame Ward capability.</summary>
    public FlameWardAction() : base("ability:FlameWard") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.FlameWard.Id, StatusEffects.FlameWard.Persistence)];
}
