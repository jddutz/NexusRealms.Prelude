using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies one-turn, one-point resistance to Ice damage per incoming attack.</summary>
public sealed record FrostWardAction : AbilityAction
{
    /// <summary>Creates the Frost Ward capability.</summary>
    public FrostWardAction() : base("ability:FrostWard") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.FrostWard.Id, StatusEffects.FrostWard.Persistence)];
}
