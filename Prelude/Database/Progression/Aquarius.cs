namespace NexusRealms.Prelude.Database.Progression;

public static class Aquarius
{
    /// <summary>
    /// Gets the Barrier ability data.
    /// </summary>
    public static AbilityData Barrier { get; } =
        new()
        {
            Id = new(nameof(Barrier)),
            ActionId = "ability:SoundBarrier",
            Name = "Barrier",
            Description =
                "Prevents all enemies in the back row from using magical abilities for 2 turns",
            Icon = new("abilities.Barrier.png"),
            TurnCost = 1f,
            Duration = 2,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.All),
        };

    public static AbilityData Bewilder { get; } =
        new()
        {
            Id = new(nameof(Bewilder)),
            ActionId = "ability:Bewilder",
            Name = "Bewilder",
            Description = "Renders the target incapable of using Focus for 2 turns",
            Icon = new("abilities.Bewilder.png"),
            TurnCost = 0.5f,
            Duration = 2,
            FocusCost = 0,
            Flags = ActionFlags.Visual | ActionFlags.Magical,
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData Cleanse { get; } =
        new()
        {
            Id = new(nameof(Cleanse)),
            ActionId = "ability:Cleanse",
            Name = "Cleanse",
            Description = "Reduces the duration of all negative status effects by 1 turn",
            Icon = new("abilities.Cleanse.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData CoolingBreeze { get; } =
        new()
        {
            Id = new(nameof(CoolingBreeze)),
            ActionId = "ability:CoolingBreeze",
            Name = "Cooling Breeze",
            Description = "Negates all Fire damage for 3 turns",
            Icon = new("abilities.CoolingBreeze.png"),
            TurnCost = 1f,
            Duration = 3,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Dowse { get; } =
        new()
        {
            Id = new(nameof(Dowse)),
            ActionId = "ability:Dowse",
            Name = "Dowse",
            Description = "Gain 1 Focus instantly",
            Icon = new("abilities.Dowse.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData EyeOfTheStorm { get; } =
        new()
        {
            Id = new(nameof(EyeOfTheStorm)),
            ActionId = "ability:EyeOfTheStorm",
            Name = "Eye of the Storm",
            Description = "Replace normal Focus gain with 1, 1, 2, 3, 5, 8 over six turns",
            Icon = new("abilities.EyeOfTheStorm.png"),
            TurnCost = 1f,
            Duration = 6,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Fog { get; } =
        new()
        {
            Id = new(nameof(Fog)),
            ActionId = "ability:Fog",
            Name = "Fog",
            Description = "Applies Blind to all enemies for 2 turns",
            Icon = new("abilities.Fog.png"),
            TurnCost = 1f,
            Duration = 2,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.All),
        };

    public static AbilityData Gale { get; } =
        new()
        {
            Id = new(nameof(Gale)),
            ActionId = "ability:Gale",
            Name = "Gale",
            Description = "Push all enemies back one row",
            Icon = new("abilities.Gale.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Cone),
        };

    public static AbilityData Headwind { get; } =
        new()
        {
            Id = new(nameof(Headwind)),
            ActionId = "ability:Headwind",
            Name = "Headwind",
            Description = "Multiply the target enemy's Turn costs by 1.5 for 3 turns",
            Icon = new("abilities.Headwind.png"),
            Duration = 3,
            Flags = ActionFlags.Visual | ActionFlags.Magical,
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData Heal { get; } =
        new()
        {
            Id = new(nameof(Heal)),
            ActionId = "ability:Heal",
            Name = "Heal",
            Description = "Restore 3 Health",
            Icon = new("abilities.Heal.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Hurricane { get; } =
        new()
        {
            Id = new(nameof(Hurricane)),
            ActionId = "ability:Hurricane",
            Name = "Hurricane",
            Description = "Deal 2 Basic and 1 Electrical damage to all enemies",
            Icon = new("abilities.Hurricane.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.All),
        };

    public static AbilityData Levitate { get; } =
        new()
        {
            Id = new(nameof(Levitate)),
            ActionId = "ability:Levitate",
            Name = "Levitate",
            Description = "Gain +1 Elevation for one turn per Focus remaining after paying 1 Focus",
            Icon = new("abilities.Levitate.png"),
            TurnCost = 1f,
            FocusCost = 1,
            MinimumFocus = 2,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData PressureShield { get; } =
        new()
        {
            Id = new(nameof(PressureShield)),
            ActionId = "ability:PressureShield",
            Name = "Pressure Shield",
            Description = "Reduces incoming physical damage by 1",
            Icon = new("abilities.PressureShield.png"),
            TurnCost = 1f,
            FocusCost = 1,
            MinimumFocus = 2,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Purge { get; } =
        new()
        {
            Id = new(nameof(Purge)),
            ActionId = "ability:Purge",
            Name = "Purge",
            Description = "Removes all positive and negative status effects",
            Icon = new("abilities.Purge.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Single, TargetRelationship.Any),
        };

    public static AbilityData Sandstorm { get; } =
        new()
        {
            Id = new(nameof(Sandstorm)),
            ActionId = "ability:Sandstorm",
            Name = "Sandstorm",
            Description = "Inflicts 1 Slashing damage and Blind for 2 turns to all enemies",
            Icon = new("abilities.Sandstorm.png"),
            TurnCost = 1f,
            Duration = 2,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.Cone),
        };

    public static AbilityData Silence { get; } =
        new()
        {
            Id = new(nameof(Silence)),
            ActionId = "ability:Silence",
            Name = "Silence",
            Description = "Prevents the target enemy from using magical abilities for 2 turns",
            Icon = new("abilities.Silence.png"),
            TurnCost = 1f,
            Duration = 2,
            FocusCost = 0,
            Flags = ActionFlags.Visual | ActionFlags.Magical,
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData Tailwind { get; } =
        new()
        {
            Id = new(nameof(Tailwind)),
            ActionId = "ability:Tailwind",
            Name = "Tailwind",
            Description = "Push all enemies ahead one row",
            Icon = new("abilities.Tailwind.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.All),
        };

    public static AbilityData Twister { get; } =
        new()
        {
            Id = new(nameof(Twister)),
            ActionId = "ability:Twister",
            Name = "Twister",
            Description = "Deal 1 Crushing damage and Delay all enemies by 0.5 turns",
            Icon = new("abilities.Twister.png"),
            TurnCost = 1f,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.All),
        };

    public static AbilityData Whirlwind { get; } =
        new()
        {
            Id = new(nameof(Whirlwind)),
            ActionId = "ability:Whirlwind",
            Name = "Whirlwind",
            Description = "Attack all enemies in the front row",
            Icon = new("abilities.Whirlwind.png"),
            TurnCost = 1.5f,
            FocusCost = 3,
            Targeting = new(TargetShape.FrontRow),
        };

    public static AbilityData WindShear { get; } =
        new()
        {
            Id = new(nameof(WindShear)),
            ActionId = "ability:WindShear",
            Name = "Wind Shear",
            Description = "Multiply Turn costs by 1.5 for all enemies in the front row for 3 turns",
            Icon = new("abilities.WindShear.png"),
            TurnCost = 1f,
            Duration = 3,
            FocusCost = 0,
            Flags = ActionFlags.Magical,
            Targeting = new(TargetShape.All),
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
        Barrier,
        Tailwind,
        Twister,
        Whirlwind,
        WindShear,
    ];

    public static ProgressionGraph Graph { get; } =
        new(
            ZodiacSignId.Aquarius,
            [
                // North — Control
                new(Silence.Id, null, 0.00f, 0.42f),
                new(Fog.Id, Silence.Id, 0.28f, 0.42f),
                new(Bewilder.Id, Fog.Id, 0.42f, 0.78f),
                new(Sandstorm.Id, Fog.Id, 0.56f, 0.28f),
                new(Levitate.Id, Silence.Id, 0.14f, 0.84f),
                new(Barrier.Id, Silence.Id, -0.14f, 0.84f),
                // East — Restoration
                new(Heal.Id, null, 0.28f, -0.00f),
                new(Cleanse.Id, Heal.Id, 0.56f, -0.28f),
                new(Dowse.Id, Cleanse.Id, 0.56f, -0.84f),
                new(Purge.Id, Cleanse.Id, 0.84f, -0.28f),
                new(EyeOfTheStorm.Id, Dowse.Id, 0.84f, -0.84f),
                // South — Protection
                new(CoolingBreeze.Id, null, 0.00f, -0.42f),
                new(PressureShield.Id, CoolingBreeze.Id, 0.00f, -0.84f),
                // West — Wind
                new(Tailwind.Id, null, -0.28f, 0.00f),
                new(Gale.Id, Tailwind.Id, -0.28f, -0.56f),
                new(Twister.Id, Gale.Id, -0.56f, -0.56f),
                new(Hurricane.Id, Twister.Id, -0.84f, -0.56f),
                new(Headwind.Id, Tailwind.Id, -0.56f, -0.00f),
                new(WindShear.Id, Tailwind.Id, -0.56f, 0.56f),
                new(Whirlwind.Id, WindShear.Id, -0.84f, 0.56f),
            ],
            Definitions
        );
}
