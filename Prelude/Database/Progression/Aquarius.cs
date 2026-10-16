namespace NexusRealms.Prelude.Database.Progression;

public class Aquarius
{
    public static AbilityData Bewilder { get; } =
        new()
        {
            Id = new(nameof(Bewilder)),
            Name = "Bewilder",
            Description = "Renders the target incapable of using Focus for one turn",
            Icon = new("abilities.Bewilder.png"),
            Actions = [],
        };

    public static AbilityData Cleanse { get; } =
        new()
        {
            Id = new(nameof(Cleanse)),
            Name = "Cleanse",
            Description = "Reduces the duration all negative status effect by 1 turn",
            Icon = new("abilities.Cleanse.png"),
            Actions = [],
        };

    public static AbilityData CoolingBreeze { get; } =
        new()
        {
            Id = new(nameof(CoolingBreeze)),
            Name = "Cooling Breeze",
            Description = "Negates all Fire damage for 1 turn",
            Icon = new("abilities.CoolingBreeze.png"),
            Actions = [],
        };

    public static AbilityData Dowse { get; } =
        new()
        {
            Id = new(nameof(Dowse)),
            Name = "Dowse",
            Description = "Gain 1 Focus instantly",
            Icon = new("abilities.Dowse.png"),
            Actions = [],
        };

    public static AbilityData EyeOfTheStorm { get; } =
        new()
        {
            Id = new(nameof(EyeOfTheStorm)),
            Name = "Eye of the Storm",
            Description = "Replace normal Focus gain with 1, 1, 2, 3, 5, 8 over six turns",
            Icon = new("abilities.EyeOfTheStorm.png"),
            Actions = [],
        };

    public static AbilityData Fog { get; } =
        new()
        {
            Id = new(nameof(Fog)),
            Name = "Fog",
            Description = "Applies Blind to all enemies",
            Icon = new("abilities.Fog.png"),
            Actions = [],
        };

    public static AbilityData Gale { get; } =
        new()
        {
            Id = new(nameof(Gale)),
            Name = "Gale",
            Description = "Push all enemies back one row",
            Icon = new("abilities.Gale.png"),
            Actions = [],
        };

    public static AbilityData Headwind { get; } =
        new()
        {
            Id = new(nameof(Headwind)),
            Name = "Headwind",
            Description = "Slow the target enemy for 1 turn",
            Icon = new("abilities.Headwind.png"),
            Actions = [],
        };

    public static AbilityData Heal { get; } =
        new()
        {
            Id = new(nameof(Heal)),
            Name = "Heal",
            Description = "Restore 3 Health",
            Icon = new("abilities.Heal.png"),
            Actions = [],
        };

    public static AbilityData Hurricane { get; } =
        new()
        {
            Id = new(nameof(Hurricane)),
            Name = "Hurricane",
            Description = "Deal 1 Crushing and 1 Electrical damage to all enemies",
            Icon = new("abilities.Hurricane.png"),
            Actions = [],
        };

    public static AbilityData Levitate { get; } =
        new()
        {
            Id = new(nameof(Levitate)),
            Name = "Levitate",
            Description = "Float above the enemy out of melee range (gain +1 Elevation)",
            Icon = new("abilities.Levitate.png"),
            Actions = [],
        };

    public static AbilityData PressureShield { get; } =
        new()
        {
            Id = new(nameof(PressureShield)),
            Name = "Pressure Shield",
            Description = "Reduces incoming physical damage by 1",
            Icon = new("abilities.PressureShield.png"),
            Actions = [],
        };

    public static AbilityData Purge { get; } =
        new()
        {
            Id = new(nameof(Purge)),
            Name = "Purge",
            Description = "Removes all status positive and negative status effects",
            Icon = new("abilities.Purge.png"),
            Actions = [],
        };

    public static AbilityData Sandstorm { get; } =
        new()
        {
            Id = new(nameof(Sandstorm)),
            Name = "Sandstorm",
            Description = "Inflicts 1 Slashing damage and Blind to all enemies",
            Icon = new("abilities.Sandstorm.png"),
            Actions = [],
        };

    public static AbilityData Silence { get; } =
        new()
        {
            Id = new(nameof(Silence)),
            Name = "Silence",
            Description = "Prevents the target enemy from using magic for 1 turn",
            Icon = new("abilities.Silence.png"),
            Actions = [],
        };

    public static AbilityData SoundBarrier { get; } =
        new()
        {
            Id = new(nameof(SoundBarrier)),
            Name = "Sound Barrier",
            Description =
                "Prevent all enemies on the back rank from using magical abilities for one turn",
            Icon = new("abilities.SoundBarrier.png"),
            Actions = [],
        };

    public static AbilityData Tailwind { get; } =
        new()
        {
            Id = new(nameof(Tailwind)),
            Name = "Tailwind",
            Description = "Push all enemies ahead one row",
            Icon = new("abilities.Tailwind.png"),
            Actions = [],
        };

    public static AbilityData Twister { get; } =
        new()
        {
            Id = new(nameof(Twister)),
            Name = "Twister",
            Description = "Deal 1 Crushing damage and Delay all enemies by 0.5 turns",
            Icon = new("abilities.Twister.png"),
            Actions = [],
        };

    public static AbilityData Whirlwind { get; } =
        new()
        {
            Id = new(nameof(Whirlwind)),
            Name = "Whirlwind",
            Description = "Attack all enemies in the front row",
            Icon = new("abilities.Whirlwind.png"),
            Actions = [],
        };

    public static AbilityData WindShear { get; } =
        new()
        {
            Id = new(nameof(WindShear)),
            Name = "Wind Shear",
            Description = "Slow all enemies in the front row for 1 turn",
            Icon = new("abilities.WindShear.png"),
            Actions = [],
        };

    /// <summary>Authored ability definitions, independent of progression placement.</summary>
    public static ImmutableArray<AbilityData> Definitions { get; } =
    [
        Bewilder,
        Cleanse,
        CoolingBreeze,
        Dowse,
        EyeOfTheStorm,
        Fog,
        Gale,
        Headwind,
        Heal,
        Hurricane,
        Levitate,
        PressureShield,
        Purge,
        Sandstorm,
        Silence,
        SoundBarrier,
        Tailwind,
        Twister,
        Whirlwind,
        WindShear,
    ];

    public static ProgressionGraph Graph { get; } =
        new(
            ZodiacSign.Aquarius,
            [
                new(Bewilder.Id, Fog.Id, 0.84f, 0.70f),
                new(Cleanse.Id, Heal.Id, 0.56f, -0.28f),
                new(CoolingBreeze.Id, null, 0.00f, -0.42f),
                new(Dowse.Id, Cleanse.Id, 0.56f, -0.84f),
                new(EyeOfTheStorm.Id, Dowse.Id, 0.84f, -0.84f),
                new(Fog.Id, Silence.Id, 0.28f, 0.42f),
                new(Gale.Id, Tailwind.Id, -0.28f, -0.56f),
                new(Headwind.Id, Tailwind.Id, -0.56f, -0.00f),
                new(Heal.Id, null, 0.28f, -0.00f),
                new(Hurricane.Id, Twister.Id, -0.84f, -0.56f),
                new(Levitate.Id, Silence.Id, 0.14f, 0.84f),
                new(PressureShield.Id, CoolingBreeze.Id, 0.00f, -0.84f),
                new(Purge.Id, Cleanse.Id, 0.84f, -0.28f),
                new(Sandstorm.Id, Fog.Id, 0.56f, 0.28f),
                new(Silence.Id, null, 0.00f, 0.42f),
                new(SoundBarrier.Id, Silence.Id, -0.14f, 0.84f),
                new(Tailwind.Id, null, -0.28f, 0.00f),
                new(Twister.Id, Gale.Id, -0.56f, -0.56f),
                new(Whirlwind.Id, WindShear.Id, -0.84f, 0.56f),
                new(WindShear.Id, Tailwind.Id, -0.56f, 0.56f),
            ]
        );
}
