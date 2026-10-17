namespace NexusRealms.Prelude.Actions;

/// <summary>Runtime behavior, independent of the progression ability that unlocks it.</summary>
public abstract record AbilityAction : ActionData
{
    protected AbilityAction(ActionId id)
    {
        Id = id.Value;
        Effects = [];
        Damage = new();
    }

    /// <summary>Unfinished scaffolds cannot be selected or committed.</summary>
    public virtual bool IsImplemented => true;

    /// <summary>Pure recipient selection, shared by commitment and simulation.</summary>
    public virtual ImmutableArray<string> SelectTargets(CombatActionResolver runtime, string sourceId, string? targetId)
        => Targeting.Select(runtime, sourceId, targetId);

    /// <summary>Pure ordered operations executed by the shared resolver, including previews.</summary>
    public abstract ImmutableArray<ActionEffect> Execute();

    /// <summary>Pure execution planning for actions whose operations depend on the current loadout.</summary>
    public virtual ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId) => Execute();
    public virtual bool CanUse(Combat.CombatLoadout loadout) => true;
    public virtual WeaponRange? GetMeleeRange(Combat.CombatLoadout loadout) => MeleeRange;

    public sealed override ImmutableArray<ActionEffect> BuildEffects() => Execute();
}

/// <summary>Registered identity awaiting an authored execution implementation.</summary>
public sealed record ScaffoldAbilityAction(ActionId ActionId) : AbilityAction(ActionId)
{
    public override bool IsImplemented => false;
    // TODO: replace each scaffold with its named AbilityAction implementation.
    public override ImmutableArray<ActionEffect> Execute() => [];
}
