namespace NexusRealms.Prelude.Database;

/// <summary>Status definitions shared by detailed descriptions and compact HUD icons.</summary>
public class StatusEffects
{
    public static StatusEffectData Guarded =>
        new()
        {
            Id = "guarded",
            Name = "Guarded",
            Description = "A protective status effect.",
            Duplicates = DuplicateStatusRule.Refresh,
            Icon = new("stats.shield.png"),
        };

    public static StatusEffectData Inspired =>
        new()
        {
            Id = "inspired",
            Name = "Inspired",
            Description = "An encouraging status effect.",
            Duplicates = DuplicateStatusRule.Refresh,
            Icon = new("stats.arrow_up.png"),
        };

    public static StatusEffectData Focused =>
        new()
        {
            Id = "focused",
            Name = "Focused",
            Description = "A concentration status effect.",
            Duplicates = DuplicateStatusRule.Refresh,
            Icon = new("stats.moon_star.png"),
        };

    /// <summary>Three-turn Fire protection; repeated applications refresh its lifetime.</summary>
    public static StatusEffectData CoolingBreeze { get; } = new()
    {
        Id = "cooling-breeze",
        Name = "Cooling Breeze",
        Description = "Negates Fire damage for three turns.",
        Icon = new("abilities.CoolingBreeze.png"),
        Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 3),
        Tags = ["positive"],
        Rules = [new NegateEffect(new(DamageType.Fire))],
    };

    public static StatusEffectData Elevated { get; } = new()
    {
        Id = "elevated", Name = "Elevated",
        Description = "Raised above short-range melee and effects requiring ground contact.",
        Icon = new("abilities.Levitate.png"),
        Duplicates = DuplicateStatusRule.Refresh,
        Elevation = 1,
        Persistence = new(StatusClock.Timeline, 1),
        Tags = ["positive"],
    };

    public static StatusEffectData PressureShield { get; } = new()
    {
        Id = "pressure-shield", Name = "Pressure Shield",
        Description = "Reduces incoming Physical damage by one per attack.",
        Icon = new("abilities.PressureShield.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 1), Tags = ["positive"],
        Rules = [new ResistEffect(new(Category: DamageCategory.Physical), 1, ShareAcrossTypes: true)],
    };

    /// <summary>Reduces Physical damage by one per attack for three turns.</summary>
    public static StatusEffectData Braced { get; } = new()
    {
        Id = "braced", Name = "Brace",
        Description = "Reduces incoming Physical damage by one per attack for three turns.",
        Icon = new("abilities.Brace.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 3), Tags = ["positive"],
        Rules = [new ResistEffect(new(Category: DamageCategory.Physical), 1, ShareAcrossTypes: true)],
    };

    /// <summary>Reduces Fire damage by one per incoming attack for one timeline turn.</summary>
    public static StatusEffectData FlameWard { get; } = new()
    {
        Id = "flame-ward", Name = "Flame Ward",
        Description = "Reduces incoming Fire damage by one per attack for one turn.",
        Icon = new("abilities.FlameWard.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 1), Tags = ["positive"],
        Rules = [new ResistEffect(new(DamageType.Fire), 1)],
    };

    /// <summary>Reduces Fire, Ice, and Lightning damage by one per incoming attack for one timeline turn.</summary>
    public static StatusEffectData ElementalWard { get; } = new()
    {
        Id = "elemental-ward", Name = "Elemental Ward",
        Description = "Reduces incoming Fire, Ice, and Lightning damage by one per attack for one turn.",
        Icon = new("abilities.ElementalAegis.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 1), Tags = ["positive"],
        Rules =
        [
            new ResistEffect(new(DamageType.Fire), 1),
            new ResistEffect(new(DamageType.Ice), 1),
            new ResistEffect(new(DamageType.Lightning), 1),
        ],
    };

    /// <summary>Reduces Ice damage by one per incoming attack for one timeline turn.</summary>
    public static StatusEffectData FrostWard { get; } = new()
    {
        Id = "frost-ward", Name = "Frost Ward",
        Description = "Reduces incoming Ice damage by one per attack for one turn.",
        Icon = new("abilities.FrostWard.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 1), Tags = ["positive"],
        Rules = [new ResistEffect(new(DamageType.Ice), 1)],
    };

    /// <summary>Reduces Lightning damage by one per incoming attack for one timeline turn.</summary>
    public static StatusEffectData StormWard { get; } = new()
    {
        Id = "storm-ward", Name = "Storm Ward",
        Description = "Reduces incoming Lightning damage by one per attack for one turn.",
        Icon = new("abilities.StormWard.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 1), Tags = ["positive"],
        Rules = [new ResistEffect(new(DamageType.Lightning), 1)],
    };

    /// <summary>Causes the attacker to lose two Focus after dealing Physical damage to the status owner.</summary>
    public static StatusEffectData Recoil { get; } = new()
    {
        Id = "recoil", Name = "Recoil",
        Description = "Attackers lose 2 Focus after dealing Physical damage to this character.",
        Icon = new("abilities.Recoil.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["positive"],
        OnDamageTaken = new(
            new(Category: DamageCategory.Physical),
            [new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 2) { Recipients = RecipientScope.Source }]
        ),
    };

    /// <summary>Causes the status owner to lose two Focus after dealing Elemental damage.</summary>
    public static StatusEffectData ElementalRecoil { get; } = new()
    {
        Id = "elemental-recoil", Name = "Elemental Recoil",
        Description = "Loses 2 Focus after dealing Elemental damage.",
        Icon = new("abilities.ElementalRecoil.png"), Duplicates = DuplicateStatusRule.Refresh,
        Tags = ["negative"],
        OnDamageDealt = new(
            new(Category: DamageCategory.Elemental),
            [new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 2) { Recipients = RecipientScope.Source }]
        ),
    };

    /// <summary>Restores one Health at the start of every turn while active.</summary>
    public static StatusEffectData Regen { get; } = new()
    {
        Id = "regen", Name = "Regen",
        Description = "Regenerates 1 Health at the start of each turn for the remainder of the battle.",
        Icon = new("abilities.Regen.png"), Duplicates = DuplicateStatusRule.Refresh,
        Tags = ["positive"], TurnStartEffects = [new RestoreEffect(1)],
    };

    /// <summary>Evades the next incoming melee action.</summary>
    public static StatusEffectData Aegis { get; } = new()
    {
        Id = "aegis", Name = "Aegis",
        Description = "Negates the next incoming melee attack.",
        Icon = new("abilities.Aegis.png"), Duplicates = DuplicateStatusRule.Refresh,
        Tags = ["positive"],
        Persistence = new(Uses: 1, UseEvent: StatusUseEvent.IncomingAction,
            UseFilter: new(RequiresActionFlags: ActionFlags.Melee)),
        Rules = [new EvadeEffect(new(RequiresActionFlags: ActionFlags.Melee))],
    };

    /// <summary>Negates Elemental damage from the next incoming action.</summary>
    public static StatusEffectData ElementalAegis { get; } = new()
    {
        Id = "elemental-aegis", Name = "Elemental Aegis",
        Description = "Negates all Elemental damage from the next incoming attack.",
        Icon = new("abilities.ElementalAegis.png"), Duplicates = DuplicateStatusRule.Refresh,
        Tags = ["positive"],
        Persistence = new(
            Uses: 1,
            UseEvent: StatusUseEvent.IncomingAction,
            UseDamageFilter: new(Category: DamageCategory.Elemental)
        ),
        Rules = [new NegateEffect(new(Category: DamageCategory.Elemental))],
    };

    /// <summary>Redirects incoming Elemental damage to every living enemy in the owner's front row for one turn.</summary>
    public static StatusEffectData Refract { get; } = new()
    {
        Id = "refract", Name = "Refract",
        Description = "Redirects incoming Elemental damage to every living enemy in the front row for one turn.",
        Icon = new("abilities.Refract.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.RecipientTurnStart, 1), Tags = ["positive"],
        Rules = [new RedirectDamageEffect(
            new(Category: DamageCategory.Elemental),
            new(TargetShape.FrontRow)
        )],
    };

    /// <summary>Uses one equipped-weapon damage profile to resist the next incoming melee action.</summary>
    public static StatusEffectData Parry { get; } = new()
    {
        Id = "parry", Name = "Parry",
        Description = "Reduces the next incoming melee attack by the damage types and amounts of the equipped weapons.",
        Icon = new("abilities.Parry.png"), Duplicates = DuplicateStatusRule.Refresh,
        Tags = ["positive"],
        Persistence = new(UseEvent: StatusUseEvent.IncomingAction, Uses: 1,
            UseFilter: new(RequiresActionFlags: ActionFlags.Melee)),
    };

    public static StatusEffectData Bewildered { get; } = new()
    {
        Id = "bewildered", Name = "Bewildered",
        Description = "Cannot spend Focus for two turns.",
        Icon = new("abilities.Bewilder.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(SpendsResource: ResourceKind.Focus))],
    };

    public static StatusEffectData EyeOfTheStorm { get; } = new()
    {
        Id = "eye-of-the-storm", Name = "Eye of the Storm",
        Description = "Focus gains become 1, 1, 2, 3, 5, 8 at the next six timeline boundaries.",
        Icon = new("abilities.EyeOfTheStorm.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 6), Tags = ["positive"],
        FocusGainBonuses = [0, 0, 1, 2, 4, 7],
    };

    public static StatusEffectData Blind { get; } = new()
    {
        Id = "blind", Name = "Blind", Description = "Cannot use actions that require sight.",
        Icon = new("abilities.Fog.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(RequiresActionFlags: ActionFlags.Visual))],
    };

    /// <summary>Blocks magical affinity independently of speech, sight and Focus costs.</summary>
    public static StatusEffectData Silenced { get; } = new()
    {
        Id = "silenced", Name = "Silenced", Description = "Cannot use magical abilities for two turns.",
        Icon = new("abilities.Silence.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(RequiresActionFlags: ActionFlags.Magical))],
    };

    /// <summary>Prevents movement and Melee actions for two timeline turns.</summary>
    public static StatusEffectData Held { get; } = new()
    {
        Id = "held", Name = "Held", Description = "Cannot change position or use Melee actions for two turns.",
        Icon = new("abilities.Hold.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules =
        [
            new PreventOperationEffect(new(EffectVerb.Move)),
            new RestrictEffect(new(RequiresActionFlags: ActionFlags.Melee)),
        ],
    };

    /// <summary>Two-turn Fear that prevents actions with melee affinity.</summary>
    public static StatusEffectData Fear { get; } = new()
    {
        Id = "fear", Name = "Fear", Description = "Cannot use melee actions for two turns.",
        Icon = new("abilities.Bellow.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(RequiresActionFlags: ActionFlags.Melee))],
    };

    /// <summary>Two-turn Berserk that permits only actions with melee affinity.</summary>
    public static StatusEffectData Berserk { get; } = new()
    {
        Id = "berserk", Name = "Berserk", Description = "Can only use actions with the Melee flag.",
        Icon = new("stats.arrow_up.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(MissingActionFlags: ActionFlags.Melee))],
    };

    /// <summary>Default Slow; authored applications choose their own lifetime.</summary>
    public static StatusEffectData Slow { get; } = new()
    {
        Id = "slow", Name = "Slow", Description = "Multiplies Turn costs by 1.5.",
        Icon = new("abilities.Headwind.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 3), Tags = ["negative"],
        Rules = [new TurnCostMultiplierEffect(1.5f)],
    };

    /// <summary>Default Haste; authored applications choose their own lifetime.</summary>
    public static StatusEffectData Haste { get; } = new()
    {
        Id = "haste", Name = "Haste", Description = "Multiplies Turn costs by 0.75.",
        Icon = new("stats.arrow_up.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 3), Tags = ["positive"],
        Rules = [new TurnCostMultiplierEffect(0.75f)],
    };

    /// <summary>One-turn stackable 25% action speed boost granted by Fury.</summary>
    public static StatusEffectData Fury { get; } = new()
    {
        Id = "fury", Name = "Fury", Description = "Multiplies Turn costs by 0.75 during the caster's next turn.",
        Icon = new("abilities.Fury.png"), Duplicates = DuplicateStatusRule.Stack,
        Persistence = new(StatusClock.RecipientTurnStart, 2), Tags = ["positive"],
        Rules = [new TurnCostMultiplierEffect(0.75f)],
    };

    /// <summary>Adds one Basic damage to each Melee attack for two turns.</summary>
    public static StatusEffectData Onslaught { get; } = new()
    {
        Id = "onslaught", Name = "Onslaught", Description = "Melee attacks deal 1 additional Basic damage.",
        Icon = new("abilities.Onslaught.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["positive"],
        Rules =
        [
            new ModifyEffect(
                new(EffectVerb.Damage, DamageType.Basic, ActionFlags.Melee, RecipientScope.Source),
                NumericalField.DamageAmount,
                1
            ),
        ],
    };

    /// <summary>Doubles Crushing damage from Melee attacks; each independent stack lasts three timeline turns.</summary>
    public static StatusEffectData Gigas { get; } = new()
    {
        Id = "gigas", Name = "Gigas", Description = "Doubles Crushing damage from Melee attacks.",
        Icon = new("abilities.Gigas.png"), Duplicates = DuplicateStatusRule.Stack,
        Persistence = new(StatusClock.Timeline, 3), Tags = ["positive"],
        Rules =
        [
            new DamageMultiplierEffect(
                new(EffectVerb.Damage, DamageType.Crushing, ActionFlags.Melee, RecipientScope.Source),
                2
            ),
        ],
    };

    public static FrozenDictionary<StatusEffectId, StatusEffectData> All { get; } =
        new[] { Guarded, Inspired, Focused, CoolingBreeze, Elevated, PressureShield, Braced, ElementalWard, FlameWard, FrostWard, StormWard, Recoil, ElementalRecoil, Regen, Aegis, ElementalAegis, Refract, Parry, Bewildered, EyeOfTheStorm, Blind, Silenced, Held, Fear, Berserk, Slow, Haste, Fury, Onslaught, Gigas }.ToFrozenDictionary(status => status.Id);
}
