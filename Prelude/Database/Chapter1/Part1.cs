namespace NexusRealms.Prelude.Database.Chapter1;

/// <summary>
/// Provides the story graph for the first part of Chapter 1.
/// </summary>
public sealed record Part1 : StoryGraph
{
    public const string SubgraphId = "ch1_p1";

    public static readonly CharacterData AlleywayThug1 = new()
    {
        Id = new($"{SubgraphId}_{nameof(AlleywayThug1)}"),
        Name = "Alleyway Thug #1",
        Artwork = new("characters.alley_thug_1.png"),
        Portrait = new("portraits.alley_thug_1.png"),
        Items =
        [
            Combat.WeaponCatalog.Create(Combat.WeaponType.Clubs, "thug-club", "Club", "Right hand"),
        ],
        Equipment = ImmutableDictionary<string, string>.Empty.Add("Right hand", "thug-club"),
        Actions = [new WeaponAttackAction("club-attack", "thug-club")],
    };

    public static readonly CharacterData AlleywayThug2 = new()
    {
        Id = new($"{SubgraphId}_{nameof(AlleywayThug2)}"),
        Name = "Alleyway Thug #2",
        Artwork = new("characters.alley_thug_2.png"),
        Portrait = new("portraits.alley_thug_2.png"),
        Items =
        [
            Combat.WeaponCatalog.Create(
                Combat.WeaponType.Pistol,
                "thug-pistol",
                "Pistol",
                "Right hand"
            ),
            Combat.WeaponCatalog.CreateAmmunition(AmmunitionType.Bullet, "thug-bullets", 6),
        ],
        Equipment = ImmutableDictionary<string, string>.Empty.Add("Right hand", "thug-pistol"),
        Actions =
        [
            new WeaponAttackAction("pistol-attack", "thug-pistol")
            {
                ReloadAnimation = "Reload",
                Classifications = ["weapon", "ranged"],
            },
        ],
    };

    public static readonly CharacterData AlleywayThug3 = new()
    {
        Id = new($"{SubgraphId}_{nameof(AlleywayThug3)}"),
        Name = "Alleyway Thug #3",
        Artwork = new("characters.alley_thug_3.png"),
        Portrait = new("portraits.alley_thug_3.png"),
        Items =
        [
            Combat.WeaponCatalog.Create(
                Combat.WeaponType.Rock,
                "thug-rocks",
                "Rock",
                "Right hand"
            ) with
            {
                Quantity = 3,
            },
        ],
        Strategy = new(StrategyId.Protective) { ProtectAllies = true },
        StrategyTransitions =
        [
            new(
                "last-stand",
                new(StrategyConditionKind.HealthAtMost, Health: 2),
                new(StrategyId.Aggressive)
            ),
        ],
        Actions =
        [
            new ThrowItemAction("rock-throw", "thug-rocks"),
            new ActionData
            {
                Id = "distract",
                Effects = [new DelayEffect(1)],
                Targeting = new(TargetShape.Single, TargetRelationship.OpposingTeam),
            },
        ],
    };

    public static readonly StoryNode Intro = new CombatScenario
    {
        Id = $"{SubgraphId}_{nameof(Intro)}",
        VictoryNode = $"{SubgraphId}_{nameof(Victory)}",
        DefeatNode = $"{SubgraphId}_{nameof(Defeat)}",
        EscapeNode = $"{SubgraphId}_{nameof(Escape)}",
        VictoryConditions = [],
        DefeatConditions = [new(CombatConditionKind.PlayerDefeated)],
        Background = new("background.harbor_district_dockside_dusk.png"),
        Characters =
        [
            new CharacterPlacement
            {
                CharacterId = AlleywayThug1.Id,
                Slot = FormationSlot.FrontCenter,
            },
            new CharacterPlacement
            {
                CharacterId = AlleywayThug2.Id,
                Slot = FormationSlot.BackLeft,
                RequiredForVictory = true,
            },
            new CharacterPlacement
            {
                CharacterId = AlleywayThug3.Id,
                Slot = FormationSlot.MiddleRight,
            },
        ],
    };

    /// <summary>Starts the story with ability selection before the intro encounter.</summary>
    public static readonly StoryNode Begin = new ProgressNode
    {
        Id = $"{SubgraphId}_{nameof(Begin)}",
        Chapter = 1,
        NextNode = Intro.Id,
    };

    public static readonly OutcomeNode Victory = new()
    {
        Id = $"{SubgraphId}_Victory",
        Title = "Victory",
    };
    public static readonly OutcomeNode Defeat = new()
    {
        Id = $"{SubgraphId}_Defeat",
        Title = "Defeat",
    };
    public static readonly OutcomeNode Escape = new()
    {
        Id = $"{SubgraphId}_Escape",
        Title = "Escape",
    };

    /// <summary>
    /// Creates the first part of Chapter 1.
    /// </summary>
    public Part1()
    {
        Characters = [AlleywayThug1, AlleywayThug2, AlleywayThug3];

        Nodes = [Begin, Intro, Victory, Defeat, Escape];

        Start = Begin.Id;
        End = StoryNodeId.Invalid;
    }
}
