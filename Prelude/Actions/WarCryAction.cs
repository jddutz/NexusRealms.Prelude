using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Creates an action that applies Fear to every selected opponent.</summary>
public sealed record WarCryAction : AbilityAction
{
    /// <summary>Creates the War Cry action.</summary>
    public WarCryAction() : base("ability:WarCry") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Fear.Id, StatusEffects.Fear.Persistence)];
}
