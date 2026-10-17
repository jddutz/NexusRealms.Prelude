using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies a two-turn bonus to the caster's Basic melee damage.</summary>
public sealed record OnslaughtAction() : AbilityAction("ability:Onslaught")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Onslaught.Id, StatusEffects.Onslaught.Persistence)];
}
