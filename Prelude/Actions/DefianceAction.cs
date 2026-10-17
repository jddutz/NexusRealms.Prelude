using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Removes all negative statuses and applies Berserk for one turn per status removed.</summary>
public sealed record DefianceAction() : AbilityAction("ability:Defiance")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new DispelEffect(new(Tag: "negative"))];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var negativeStatusCount = runtime.Characters[sourceId].Statuses
            .Count(status => status.Definition.Tags.Contains("negative"));
        if (negativeStatusCount == 0) return Execute();

        return
        [
            new DispelEffect(new(Tag: "negative")),
            new ApplyEffect(StatusEffects.Berserk.Id, new(StatusClock.Timeline, negativeStatusCount)),
        ];
    }
}
