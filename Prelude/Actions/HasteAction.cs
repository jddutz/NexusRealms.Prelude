using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Reduces all action Turn costs by 25 percent for three turns.</summary>
public sealed record HasteAction() : AbilityAction("ability:Haste")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Haste.Id, StatusEffects.Haste.Persistence)];
}
