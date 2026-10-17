using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies Berserk to the selected enemy row for two timeline turns.</summary>
public sealed record EnrageAction() : AbilityAction("ability:Enrage")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Berserk.Id, new(StatusClock.Timeline, 2))];
}
