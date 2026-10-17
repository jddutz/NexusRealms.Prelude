using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies Berserk to the selected enemy for two timeline turns.</summary>
public sealed record TauntAction() : AbilityAction("ability:Taunt")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Berserk.Id, new(StatusClock.Timeline, 2))];
}
