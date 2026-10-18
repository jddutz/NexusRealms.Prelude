using NexusRealms.Prelude.Database;

namespace NexusRealms.Prelude.Actions;

/// <summary>Applies a two-turn counter that drains an attacker's Focus after Physical damage.</summary>
public sealed record RecoilAction : AbilityAction
{
    /// <summary>Creates the Recoil capability.</summary>
    public RecoilAction() : base("ability:Recoil") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect(StatusEffects.Recoil.Id, StatusEffects.Recoil.Persistence)];
}
