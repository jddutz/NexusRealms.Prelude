namespace NexusRealms.Prelude.Database.Progression;

public static class Cancer
{
    public static AbilityData Aegis { get; } =
        new()
        {
            Id = new(nameof(Aegis)),
            ActionId = "ability:Aegis",
            Name = "Aegis",
            Description = "Negate all damage from the next melee attack",
            TurnCost = 0.4f,
            FocusCost = 2,
            Icon = new("abilities.Aegis.png"),
            Targeting = new(TargetShape.Self),
        };

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

    public static AbilityData Brace { get; } =
        new()
        {
            Id = new(nameof(Brace)),
            ActionId = "ability:Brace",
            Name = "Brace",
            Description = "Reduce Physical damage taken by 1 per incoming attack for 2 turns",
            Icon = new("abilities.Brace.png"),
            TurnCost = 1.0f,
            Duration = 2,
            FocusCost = 2,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData BreakArmor { get; } =
        new()
        {
            Id = new(nameof(BreakArmor)),
            ActionId = "ability:BreakArmor",
            Name = "Break Armor",
            Description = "Reduce Basic damage resistance provided by the target's armor by 1",
            Icon = new("abilities.BreakArmor.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Duration = int.MaxValue,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Deflect { get; } =
        new()
        {
            Id = new(nameof(Deflect)),
            ActionId = "ability:Deflect",
            Name = "Deflect",
            Description = "Negate all damage from the next ranged physical attack",
            TurnCost = 0.4f,
            FocusCost = 2,
            Icon = new("abilities.Deflect.png"),
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Disarm { get; } =
        new()
        {
            Id = new(nameof(Disarm)),
            ActionId = "ability:Disarm",
            Name = "Disarm",
            Description =
                "Move equipped weapons to the target's inventory, disarmed weapons cannot be re-equipped for one full turn",
            TurnCost = 1.0f,
            FocusCost = 1,
            Icon = new("abilities.Disarm.png"),
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData ElementalAegis { get; } =
        new()
        {
            Id = new(nameof(ElementalAegis)),
            ActionId = "ability:ElementalAegis",
            Name = "ElementalAegis",
            Description = "Negate all Elemental damage from the next incoming attack",
            TurnCost = 0.4f,
            FocusCost = 2,
            Icon = new("abilities.ElementalAegis.png"),
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData ElementalRecoil { get; } =
        new()
        {
            Id = new(nameof(ElementalRecoil)),
            ActionId = "ability:ElementalRecoil",
            Name = "ElementalRecoil",
            Description = "Attacker loses 2 Focus after inflicting Elemental damage",
            Icon = new("abilities.ElementalRecoil.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Targeting = new(TargetShape.Single),
        };

    /// <summary>Gets the Elemental Ward ability data.</summary>
    public static AbilityData ElementalWard { get; } =
        new()
        {
            Id = new(nameof(ElementalWard)),
            ActionId = "ability:ElementalWard",
            Name = "Elemental Ward",
            Description =
                "Reduce Fire, Ice, and Lightning damage taken by 1 per incoming attack for 1 turn",
            Icon = new("abilities.ElementalAegis.png"),
            TurnCost = 0.5f,
            Duration = 1,
            FocusCost = 1,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Gigas { get; } =
        new()
        {
            Id = new(nameof(Gigas)),
            ActionId = "ability:Gigas",
            Name = "Gigas",
            Description = "Doubles the Crushing damage of equipped weapons for 3 turns",
            Icon = new("abilities.Gigas.png"),
            TurnCost = 0.8f,
            Duration = 3,
            FocusCost = 1,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Hold { get; } =
        new()
        {
            Id = new(nameof(Hold)),
            ActionId = "ability:Hold",
            Name = "Hold",
            Description =
                "Prevents the target from changing position or using Melee actions for 2 turns",
            Icon = new("abilities.Hold.png"),
            TurnCost = 1.0f,
            Duration = 2,
            FocusCost = 0,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Parry { get; } =
        new()
        {
            Id = new(nameof(Parry)),
            ActionId = "ability:Parry",
            Name = "Parry",
            Description =
                "Reduce the next melee attack's damage using the matching damage types and values of your equipped weapon(s)",
            Icon = new("abilities.Parry.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Recoil { get; } =
        new()
        {
            Id = new(nameof(Recoil)),
            ActionId = "ability:Recoil",
            Name = "Recoil",
            Description = "Attacker loses 2 Focus after inflicting Physical damage",
            Icon = new("abilities.Recoil.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Duration = 2,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Refract { get; } =
        new()
        {
            Id = new(nameof(Refract)),
            ActionId = "ability:Refract",
            Name = "Refract",
            Description = "Incoming Elemental damage is redirected to all enemies in the front row",
            Icon = new("abilities.Refract.png"),
            TurnCost = 1.0f,
            FocusCost = 3,
            Duration = 1.0f,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Regen { get; } =
        new()
        {
            Id = new(nameof(Regen)),
            ActionId = "ability:Regen",
            Name = "Regen",
            Description = "Regenerate 1 Health per turn, for the remainder of the battle",
            Icon = new("abilities.Regen.png"),
            TurnCost = 1.0f,
            FocusCost = 3,
            Duration = int.MaxValue,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData ShieldBash { get; } =
        new()
        {
            Id = new(nameof(ShieldBash)),
            ActionId = "ability:ShieldBash",
            Name = "ShieldBash",
            Description =
                "Inflicts 2 Basic damage and delays the target's next action by 0.8 turns and removes 1 Focus",
            Icon = new("abilities.ShieldBash.png"),
            TurnCost = 1.0f,
            FocusCost = 0,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData ShieldMastery { get; } =
        new()
        {
            Id = new(nameof(ShieldMastery)),
            ActionId = "ability:ShieldMastery",
            Name = "Shield Mastery",
            Description =
                "Passive — Equipped shields reduce incoming Physical damage by an additional 1",
            Icon = new("abilities.ShieldMastery.png"),
            Passive = true,
        };

    public static AbilityData Shock { get; } =
        new()
        {
            Id = new(nameof(Shock)),
            ActionId = "ability:Shock",
            Name = "Shock",
            Description = "Target loses 3 focus",
            Icon = new("abilities.Shock.png"),
            TurnCost = 0.8f,
            FocusCost = 1,
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData Shockwave { get; } =
        new()
        {
            Id = new(nameof(Shockwave)),
            ActionId = "ability:Shockwave",
            Name = "Shockwave",
            Description =
                "Inflicts normal melee damage plus 1 Crushing damage to adjacent characters on the same row",
            Icon = new("abilities.Shockwave.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Targeting = new(TargetShape.Melee),
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

    /// <summary>Authored ability definitions owned by the Cancer progression.</summary>
    public static ImmutableArray<AbilityData> Definitions { get; } =
    [
        Aegis,
        Barrier,
        Brace,
        BreakArmor,
        Deflect,
        Disarm,
        ElementalAegis,
        ElementalRecoil,
        ElementalWard,
        Gigas,
        Hold,
        Parry,
        Recoil,
        Refract,
        Regen,
        ShieldBash,
        ShieldMastery,
        Shock,
        Shockwave,
        Silence,
    ];

    public static ProgressionGraph Graph { get; } =
        new(
            ZodiacSignId.Cancer,
            [
                // North — Physical Defense
                new(Brace.Id, null, -0.84f, -0.84f),
                new(Parry.Id, Brace.Id, -0.56f, -0.84f),
                new(Deflect.Id, Brace.Id, -0.28f, -0.84f),
                new(Recoil.Id, Brace.Id, 0.0f, -0.84f),
                new(Aegis.Id, Deflect.Id, 0.28f, -0.84f),
                new(ShieldMastery.Id, Deflect.Id, 0.56f, -0.84f),
                // East — Physical Force
                new(ShieldBash.Id, null, -0.84f, -0.28f),
                new(BreakArmor.Id, ShieldBash.Id, -0.56f, -0.28f),
                new(Shockwave.Id, ShieldBash.Id, -0.28f, -0.28f),
                new(Gigas.Id, Shockwave.Id, 0.0f, -0.28f, Rank: 12),
                // South — Elemental Defense
                new(ElementalWard.Id, null, -0.84f, 0.28f),
                new(Regen.Id, ElementalWard.Id, -0.56f, 0.28f),
                new(ElementalRecoil.Id, ElementalWard.Id, -0.28f, 0.28f),
                new(ElementalAegis.Id, ElementalRecoil.Id, 0.0f, 0.28f),
                new(Refract.Id, ElementalAegis.Id, 0.28f, 0.28f),
                // West — Disruption
                new(Hold.Id, null, -0.84f, 0.84f),
                new(Silence.Id, Hold.Id, -0.56f, 0.84f),
                new(Disarm.Id, Hold.Id, -0.28f, 0.84f),
                new(Barrier.Id, Silence.Id, 0.0f, 0.84f),
                new(Shock.Id, Disarm.Id, 0.28f, 0.84f),
            ],
            Definitions
        );
}
