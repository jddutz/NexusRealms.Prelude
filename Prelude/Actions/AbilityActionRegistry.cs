namespace NexusRealms.Prelude.Actions;

/// <summary>Maps authored action identities to runtime implementations, never back to abilities.</summary>
public sealed class AbilityActionRegistry
{
    private readonly FrozenDictionary<ActionId, AbilityAction> _implementations;

    public AbilityActionRegistry(IEnumerable<AbilityAction> implementations)
        => _implementations = implementations.ToFrozenDictionary(action => new ActionId(action.Id));

    public static AbilityActionRegistry Default { get; } = new(new AbilityAction[]
    {
        new ScaffoldAbilityAction("ability:slash"),
        new ScaffoldAbilityAction("ability:push"),
        new ScaffoldAbilityAction("ability:parry"),
        new BraceAction(),
        new ChargeAction(),
        new DualStrikeAction(),
        new GrabAction(),
        new HasteAction(),
        new KingMakerAction(),
        new PushAction(),
        new RampageAction(),
        new ScaffoldAbilityAction("ability:north-1"),
        new ScaffoldAbilityAction("ability:north-2"),
        new ScaffoldAbilityAction("ability:north-3"),
        new ScaffoldAbilityAction("ability:north-4"),
        new ScaffoldAbilityAction("ability:north-5"),
        new ScaffoldAbilityAction("ability:east-1"),
        new ScaffoldAbilityAction("ability:east-2"),
        new ScaffoldAbilityAction("ability:east-3"),
        new ScaffoldAbilityAction("ability:east-4"),
        new ScaffoldAbilityAction("ability:east-5"),
        new ScaffoldAbilityAction("ability:south-1"),
        new ScaffoldAbilityAction("ability:south-2"),
        new ScaffoldAbilityAction("ability:south-3"),
        new ScaffoldAbilityAction("ability:south-4"),
        new ScaffoldAbilityAction("ability:south-5"),
        new ScaffoldAbilityAction("ability:west-1"),
        new ScaffoldAbilityAction("ability:west-2"),
        new ScaffoldAbilityAction("ability:west-3"),
        new ScaffoldAbilityAction("ability:west-4"),
        new ScaffoldAbilityAction("ability:west-5"),
        new BewilderAction(),
        new BellowAction(),
        new BluffAction(),
        new BloodlustAction(),
        new CleanseAction(),
        new CoolingBreezeAction(),
        new DefianceAction(),
        new DowseAction(),
        new EyeOfTheStormAction(),
        new FogAction(),
        new FuryAction(),
        new GaleAction(),
        new HeadwindAction(),
        new HealAction(),
        new HurricaneAction(),
        new LevitateAction(),
        new OnslaughtAction(),
        new PressureShieldAction(),
        new PrimalRageAction(),
        new PurgeAction(),
        new RecoveryAction(),
        new SandstormAction(),
        new SilenceAction(),
        new SoundBarrierAction(),
        new TailwindAction(),
        new TwisterAction(),
        new WhirlwindAction(),
        new WindShearAction(),
    });

    /// <summary>Binds an implementation to authored presentation, optional activation metadata, and unlock requirements.</summary>
    public AbilityAction Resolve(AbilityData ability)
    {
        ArgumentNullException.ThrowIfNull(ability);
        if (!_implementations.TryGetValue(ability.ActionId, out var implementation))
            throw new ArgumentException($"Ability '{ability.Id}' references unknown action '{ability.ActionId}'.");
        if (ability.FocusCost is < 0 || ability.MinimumFocus < 0
            || ability.TurnCost is { } turnCost && (!float.IsFinite(turnCost) || turnCost <= 0))
            throw new ArgumentException($"Ability '{ability.Id}' has invalid action costs.");
        if ((ability.Flags & ~(ActionFlags.Visual | ActionFlags.Magical | ActionFlags.Melee)) != 0)
            throw new ArgumentException($"Ability '{ability.Id}' has unsupported action flags.");
        if (ability.Passive && (ability.TurnCost is not null || ability.FocusCost is not null || ability.Targeting is not null))
            throw new ArgumentException($"Passive ability '{ability.Id}' cannot define activation costs or targeting.");
        var required = implementation.Requirements.Abilities;
        var targeting = ability.Targeting ?? new TargetingProfile();
        return implementation with
        {
            Name = ability.Name,
            Icon = ability.Icon,
            TurnCost = ability.TurnCost ?? 1f,
            Targeting = targeting,
            Flags = implementation.Flags | ability.Flags,
            Passive = ability.Passive,
            Costs = ability.FocusCost is not > 0
                ? []
                : [new(new ResourceSubject(ResourceKind.Focus), ability.FocusCost.Value)],
            Requirements = implementation.Requirements with
            {
                MinimumFocus = Math.Max(implementation.Requirements.MinimumFocus, ability.MinimumFocus),
                Abilities = required.Contains(ability.Id) ? required : required.Add(ability.Id),
            },
        };
    }
}
