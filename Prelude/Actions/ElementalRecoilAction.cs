using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Marks the selected character to lose Focus after dealing Elemental damage.</summary>
public sealed record ElementalRecoilAction : AbilityAction
{
    /// <summary>Creates the Elemental Recoil capability.</summary>
    public ElementalRecoilAction() : base("ability:ElementalRecoil") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.ElementalRecoil.Id, StatusEffects.ElementalRecoil.Persistence)];
}
