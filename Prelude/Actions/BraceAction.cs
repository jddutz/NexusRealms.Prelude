using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies three turns of one-point Physical damage resistance per attack.</summary>
public sealed record BraceAction() : AbilityAction("ability:Brace")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Braced.Id, StatusEffects.Braced.Persistence)];
}
