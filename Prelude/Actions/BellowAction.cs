using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies Fear to every selected opponent.</summary>
public sealed record BellowAction() : AbilityAction("ability:Bellow")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Fear.Id, StatusEffects.Fear.Persistence)];
}
