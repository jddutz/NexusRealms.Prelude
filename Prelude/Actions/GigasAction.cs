using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies a stackable three-turn multiplier to the caster's Crushing Melee damage.</summary>
public sealed record GigasAction() : AbilityAction("ability:Gigas")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Gigas.Id, StatusEffects.Gigas.Persistence)];
}
