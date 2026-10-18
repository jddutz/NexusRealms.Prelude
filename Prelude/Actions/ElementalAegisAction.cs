using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Protects its owner from Elemental damage on the next incoming action.</summary>
public sealed record ElementalAegisAction : AbilityAction
{
    /// <summary>Creates the Elemental Aegis capability.</summary>
    public ElementalAegisAction() : base("ability:ElementalAegis") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.ElementalAegis.Id, StatusEffects.ElementalAegis.Persistence)];
}
