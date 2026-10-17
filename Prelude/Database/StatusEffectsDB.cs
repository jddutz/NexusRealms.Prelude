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
        Id = "blind", Name = "Blind", Description = "Cannot use actions that require Visual targeting.",
        Icon = new("abilities.Fog.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(RequiresTargetingFlags: TargetingFlags.Visual))],
    };

    /// <summary>Blocks magical affinity independently of speech, sight and Focus costs.</summary>
    public static StatusEffectData Silenced { get; } = new()
    {
        Id = "silenced", Name = "Silenced", Description = "Cannot use magical abilities for two turns.",
        Icon = new("abilities.Silence.png"), Duplicates = DuplicateStatusRule.Refresh,
        Persistence = new(StatusClock.Timeline, 2), Tags = ["negative"],
        Rules = [new RestrictEffect(new(RequiresTargetingFlags: TargetingFlags.Magical))],
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

    public static FrozenDictionary<StatusEffectId, StatusEffectData> All { get; } =
        new[] { Guarded, Inspired, Focused, CoolingBreeze, Elevated, PressureShield, Bewildered, EyeOfTheStorm, Blind, Silenced, Slow, Haste }.ToFrozenDictionary(status => status.Id);
}
