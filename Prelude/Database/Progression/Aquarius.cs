namespace NexusRealms.Prelude.Database.Progression;

public class Aquarius
{
    /// <summary>Executable definitions, added as each ability's combat rules are agreed.
    /// Entries override progression presentation placeholders by ability ID.</summary>
    public static ImmutableArray<AbilityData> Definitions => [];

    public static AbilityNode Bewilder =>
        new(
            "Bewilder",
            "Renders the target incapable of using Focus for one turn",
            DirectionEnum.South,
            new AbilityId(nameof(Bewilder)),
            null,
            0f,
            0f
        );

    public static AbilityNode Cleanse =>
        new(
            "Cleanse",
            "Reduces the duration all negative status effect by 1 turn",
            DirectionEnum.West,
            new AbilityId(nameof(Cleanse)),
            null,
            0f,
            0f
        );

    public static AbilityNode CoolingBreeze =>
        new(
            "Cooling Breeze",
            "Negates all Fire damage for 1 turn",
            DirectionEnum.North,
            new AbilityId(nameof(CoolingBreeze)),
            null,
            0f,
            0f
        );

    public static AbilityNode Dowse =>
        new(
            "Dowse",
            "Gain 1 Focus instantly",
            DirectionEnum.South,
            new AbilityId(nameof(Dowse)),
            null,
            0f,
            0f
        );

    public static AbilityNode EyeOfTheStorm =>
        new(
            "Eye of the Storm",
            "Replace normal Focus gain with 1, 1, 2, 3, 5, 8 over six turns",
            DirectionEnum.West,
            new AbilityId(nameof(EyeOfTheStorm)),
            null,
            0f,
            0f
        );

    public static AbilityNode Fog =>
        new(
            "Fog",
            "Applies Blind to all enemies",
            DirectionEnum.East,
            new AbilityId(nameof(Fog)),
            null,
            0f,
            0f
        );

    public static AbilityNode Gale =>
        new(
            "Gale",
            "Push all enemies back one row",
            DirectionEnum.North,
            new AbilityId(nameof(Gale)),
            null,
            0f,
            0f
        );

    public static AbilityNode Headwind =>
        new(
            "Headwind",
            "Slow the target enemy for 1 turn",
            DirectionEnum.North,
            new AbilityId(nameof(Headwind)),
            null,
            0f,
            0f
        );

    public static AbilityNode Heal =>
        new(
            "Heal",
            "Restore 3 Health",
            DirectionEnum.West,
            new AbilityId(nameof(Heal)),
            null,
            0f,
            0f
        );

    public static AbilityNode Hurricane =>
        new(
            "Hurricane",
            "Deal 1 Crushing and 1 Electrical damage to all enemies",
            DirectionEnum.East,
            new AbilityId(nameof(Hurricane)),
            null,
            0f,
            0f
        );

    public static AbilityNode Levitate =>
        new(
            "Levitate",
            "Float above the enemy out of melee range (gain +1 Elevation)",
            DirectionEnum.North,
            new AbilityId(nameof(Levitate)),
            null,
            0f,
            0f
        );

    public static AbilityNode PressureShield =>
        new(
            "Pressure Shield",
            "Reduces incoming physical damage by 1",
            DirectionEnum.East,
            new AbilityId(nameof(PressureShield)),
            null,
            0f,
            0f
        );

    public static AbilityNode Purge =>
        new(
            "Purge",
            "Removes all status positive and negative status effects",
            DirectionEnum.West,
            new AbilityId(nameof(Purge)),
            null,
            0f,
            0f
        );

    public static AbilityNode Sandstorm =>
        new(
            "Sandstorm",
            "Inflicts 1 Slashing damage and Blind to all enemies",
            DirectionEnum.East,
            new AbilityId(nameof(Sandstorm)),
            null,
            0f,
            0f
        );

    public static AbilityNode Silence =>
        new(
            "Silence",
            "Prevents the target enemy from using magic for 1 turn",
            DirectionEnum.South,
            new AbilityId(nameof(Silence)),
            null,
            0f,
            0f
        );

    public static AbilityNode SoundBarrier =>
        new(
            "Sound Barrier",
            "Prevent all enemies on the back rank from using magical abilities for one turn",
            DirectionEnum.South,
            new AbilityId(nameof(SoundBarrier)),
            null,
            0f,
            0f
        );

    public static AbilityNode Tailwind =>
        new(
            "Tailwind",
            "Push all enemies ahead one row",
            DirectionEnum.North,
            new AbilityId(nameof(Tailwind)),
            null,
            0f,
            0f
        );

    public static AbilityNode Tornado =>
        new(
            "Tornado",
            "Deal 1 Crushing damage and Delay all enemies by 0.5 turns",
            DirectionEnum.East,
            new AbilityId(nameof(Tornado)),
            null,
            0f,
            0f
        );

    public static AbilityNode Whirlwind =>
        new(
            "Whirlwind",
            "Attack all enemies in the front row",
            DirectionEnum.North,
            new AbilityId(nameof(Whirlwind)),
            null,
            0f,
            0f
        );

    public static AbilityNode WindShear =>
        new(
            "Wind Shear",
            "Slow all enemies in the front row for 1 turn",
            DirectionEnum.North,
            new AbilityId(nameof(WindShear)),
            null,
            0f,
            0f
        );

    public static ProgressionGraph Graph =>
        new(
            ZodiacSign.Aquarius,
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
                Tornado,
                Whirlwind,
                WindShear,
            ]
        );
}
