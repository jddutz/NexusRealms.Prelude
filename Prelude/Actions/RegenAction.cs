using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies the indefinite Regen status to its owner.</summary>
public sealed record RegenAction() : AbilityAction("ability:Regen")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Regen.Id, StatusEffects.Regen.Persistence)];
}
