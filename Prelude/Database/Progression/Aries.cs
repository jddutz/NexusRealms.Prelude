namespace NexusRealms.Prelude.Database.Progression;

public static class Aries
{
    public static AbilityData Bluff { get; } =
        new()
        {
            Id = new(nameof(Bluff)),
            ActionId = "ability:Bluff",
            Name = "Bluff",
            Description = "Inflicts Fear on a single target",
            Icon = new("abilities.Bluff.png"),
            TurnCost = 0.5f,
            FocusCost = 2,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Bloodlust { get; } =
        new()
        {
            Id = new(nameof(Bloodlust)),
            ActionId = "ability:Bloodlust",
            Name = "Bloodlust",
            Description = "Passive - Gain 1 Focus whenever an enemy is defeated",
            Icon = new("abilities.Bellow.png"),
            Passive = true,
        };

    public static AbilityData Brace { get; } =
        new()
        {
            Id = new(nameof(Brace)),
            ActionId = "ability:Brace",
            Name = "Brace",
            Description = "Reduce Physical damage taken by 1 per incoming attack for 3 turns",
            Icon = new("abilities.Brace.png"),
            TurnCost = 1.0f,
            FocusCost = 2,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Charge { get; } =
        new()
        {
            Id = new(nameof(Charge)),
            ActionId = "ability:Charge",
            Name = "Charge",
            Description = "Attack an opponent and push them back one row",
            Icon = new("abilities.Charge.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData KingMaker { get; } =
        new()
        {
            Id = new(nameof(KingMaker)),
            ActionId = "ability:KingMaker",
            Name = "King Maker",
            Description =
                "Consumes remaining Focus; bonus Basic damage is 1, 2, 3, 5, 8, 13, 21 at 0-6 Focus, then adds the prior two values",
            Icon = new("abilities.KingMaker.png"),
            TurnCost = 1.5f,
            FocusCost = 0,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Defiance { get; } =
        new()
        {
            Id = new(nameof(Defiance)),
            ActionId = "ability:Defiance",
            Name = "Defiance",
            Description =
                "Removes all negative status effects and replaces with Berserk for 1 turn per effect removed",
            Icon = new("abilities.Defiance.png"),
            TurnCost = 0.5f,
            FocusCost = 2,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData DualStrike { get; } =
        new()
        {
            Id = new(nameof(DualStrike)),
            ActionId = "ability:DualStrike",
            Name = "Dual Strike",
            Description = "Attack an enemy with each equipped weapon",
            Icon = new("abilities.DualStrike.png"),
            TurnCost = 1.2f,
            FocusCost = 1,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Enrage { get; } =
        new()
        {
            Id = new(nameof(Enrage)),
            ActionId = "ability:Enrage",
            Name = "Enrage",
            Description = "Applies Berserk to all targets for 2 turns",
            Icon = new("abilities.Enrage.png"),
            TurnCost = 1.0f,
            FocusCost = 3,
            Targeting = new(TargetShape.Row),
        };

    public static AbilityData Fury { get; } =
        new()
        {
            Id = new(nameof(Fury)),
            ActionId = "ability:Fury",
            Name = "Fury",
            Description =
                "Applies a 25% speed boost for 1 turn after attacking an enemy (stackable)",
            Icon = new("abilities.Fury.png"),
            TurnCost = 0.8f,
            FocusCost = 2,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Grab { get; } =
        new()
        {
            Id = new(nameof(Grab)),
            ActionId = "ability:Grab",
            Name = "Grab",
            Description = "Pull an enemy foreward one row",
            Icon = new("abilities.Grab.png"),
            TurnCost = 0.6f,
            FocusCost = 0,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Haste { get; } =
        new()
        {
            Id = new(nameof(Haste)),
            ActionId = "ability:Haste",
            Name = "Haste",
            Description = "Increase speed of all actions by 25% for 3 turns",
            Icon = new("abilities.Haste.png"),
            TurnCost = 0.5f,
            FocusCost = 1,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData Intimidate { get; } =
        new()
        {
            Id = new(nameof(Intimidate)),
            ActionId = "ability:Intimidate",
            Name = "Intimidate",
            Description = "Delays the target's next action by 1.5 turns",
            Icon = new("abilities.Intimidate.png"),
            TurnCost = 0.5f,
            FocusCost = 2,
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData Onslaught { get; } =
        new()
        {
            Id = new(nameof(Onslaught)),
            ActionId = "ability:Onslaught",
            Name = "Onslaught",
            Description = "All melee attacks gain +1 Basic damage for 2 turns",
            Icon = new("abilities.Onslaught.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Targeting = new(TargetShape.Self),
        };

    public static AbilityData PrimalRage { get; } =
        new()
        {
            Id = new(nameof(PrimalRage)),
            ActionId = "ability:PrimalRage",
            Name = "PrimalRage",
            Description = "Passive - gain 1 Focus per Basic damage taken",
            Icon = new("abilities.PrimalRage.png"),
            Passive = true,
        };

    public static AbilityData Push { get; } =
        new()
        {
            Id = new("Aries.Push"),
            ActionId = "ability:Push",
            Name = "Push",
            Description = "Push an enemy back one row",
            Icon = new("abilities.Push.png"),
            TurnCost = 0.6f,
            FocusCost = 0,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.Melee),
        };

    public static AbilityData Rampage { get; } =
        new()
        {
            Id = new(nameof(Rampage)),
            ActionId = "ability:Rampage",
            Name = "Rampage",
            Description = "Attack all enemies in the front row",
            Icon = new("abilities.Rampage.png"),
            TurnCost = 2.0f,
            FocusCost = 3,
            Flags = ActionFlags.Melee,
            Targeting = new(TargetShape.FrontRow),
        };

    public static AbilityData Recovery { get; } =
        new()
        {
            Id = new(nameof(Recovery)),
            ActionId = "ability:Recovery",
            Name = "Recovery",
            Description = "Consumes all Focus, gain 2 Health per Focus",
            Icon = new("abilities.Recovery.png"),
            TurnCost = 1.5f,
            FocusCost = 1,
            Targeting = new(TargetShape.Self),
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

    public static AbilityData Taunt { get; } =
        new()
        {
            Id = new(nameof(Taunt)),
            ActionId = "ability:Taunt",
            Name = "Taunt",
            Description = "Applies Berserk to the target for 2 turns",
            Icon = new("abilities.Taunt.png"),
            TurnCost = 1.0f,
            FocusCost = 1,
            Targeting = new(TargetShape.Single),
        };

    public static AbilityData WarCry { get; } =
        new()
        {
            Id = new(nameof(WarCry)),
            ActionId = "ability:WarCry",
            Name = "War Cry",
            Description = "Inflicts Fear on all enemies",
            Icon = new("abilities.WarCry.png"),
            TurnCost = 0.5f,
            FocusCost = 2,
            Targeting = new(TargetShape.All),
        };

    /// <summary>Authored ability definitions, independent of progression placement.</summary>
    public static ImmutableArray<AbilityData> Definitions { get; } =
    [
        Bluff,
        Bloodlust,
        Brace,
        Charge,
        Defiance,
        DualStrike,
        Enrage,
        Fury,
        Grab,
        Haste,
        Intimidate,
        KingMaker,
        Onslaught,
        PrimalRage,
        Push,
        Rampage,
        Recovery,
        Shock,
        Taunt,
        WarCry,
    ];

    public static ProgressionGraph Graph { get; } =
        new(
            ZodiacSignId.Aries,
            [
                // North — Survival
                new(Brace.Id, null, -0.84f, 0.56f),
                new(Defiance.Id, Brace.Id, -0.84f, 0.56f),
                new(Recovery.Id, Defiance.Id, -0.84f, 0.56f),
                // West — Momentum
                new(Haste.Id, null, -0.84f, 0.56f),
                new(Bloodlust.Id, Haste.Id, -0.84f, 0.56f),
                new(PrimalRage.Id, Bloodlust.Id, -0.84f, 0.56f),
                new(DualStrike.Id, Haste.Id, -0.84f, 0.56f),
                new(Fury.Id, DualStrike.Id, -0.84f, 0.56f),
                // South — Control
                new(Bluff.Id, null, -0.84f, 0.56f),
                new(WarCry.Id, Bluff.Id, -0.84f, 0.56f),
                new(Intimidate.Id, WarCry.Id, -0.84f, 0.56f),
                new(Shock.Id, Intimidate.Id, -0.84f, 0.56f),
                new(Taunt.Id, Bluff.Id, -0.84f, 0.56f),
                new(Enrage.Id, Taunt.Id, -0.84f, 0.56f),
                // East — Force
                new(Grab.Id, null, -0.84f, 0.56f),
                new(Push.Id, Grab.Id, -0.84f, 0.56f),
                new(Charge.Id, Push.Id, -0.84f, 0.56f),
                new(Onslaught.Id, Charge.Id, -0.84f, 0.56f),
                new(Rampage.Id, Onslaught.Id, -0.84f, 0.56f),
                new(KingMaker.Id, Charge.Id, -0.84f, 0.56f),
            ]
        );
}
