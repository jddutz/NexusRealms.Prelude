using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies one turn of Elemental damage redirection to the selected character.</summary>
public sealed record RefractAction : AbilityAction
{
    /// <summary>Creates the Refract capability.</summary>
    public RefractAction() : base("ability:Refract") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Refract.Id, StatusEffects.Refract.Persistence)];
}
