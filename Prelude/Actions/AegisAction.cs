namespace NexusRealms.Prelude.Actions;

/// <summary>Grants a shield-borne ward that evades the next incoming melee action.</summary>
public sealed record AegisAction : AbilityAction
{
    /// <summary>Creates the shield-gated Aegis capability.</summary>
    public AegisAction() : base("ability:Aegis")
    {
        Requirements = new()
        {
            Equipment = [new(AnyOf: [EquipmentKind.Shield])],
        };
    }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Aegis.Id, StatusEffects.Aegis.Persistence)];
}
