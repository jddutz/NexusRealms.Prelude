namespace NexusRealms.Prelude.Database.Chapter1;

/// <summary>
/// Provides the story graph for the first part of Chapter 1.
/// </summary>
public sealed record Part1StoryGraph : IStoryGraph
{
    public static readonly int ChapterId = Chapter1StoryGraph.ChapterId;
    public static readonly string SubgraphId = $"{Chapter1StoryGraph.SubgraphId}_p1";

    public IStoryGraph? Parent { get; init; }

    public CharacterData AlleywayThug1 =>
        new()
        {
            Id = new($"{SubgraphId}_{nameof(AlleywayThug1)}"),
            Name = "Alleyway Thug #1",
            Artwork = new("characters.alley_thug_1.png"),
            Portrait = new("portraits.alley_thug_1.png"),
            Items =
            [
                Combat.WeaponCatalog.Create(
                    Combat.WeaponType.Clubs,
                    "thug-club",
                    "Club",
                    "Right hand"
                ),
            ],
            Equipment = ImmutableDictionary<string, string>.Empty.Add("Right hand", "thug-club"),
            Actions = [new WeaponAttackAction("club-attack", "thug-club")],
        };

    public CharacterData AlleywayThug2 =>
        new()
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

    public CharacterData AlleywayThug3 =>
        new()
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
                    Targeting = new(TargetShape.Single),
                },
            ],
        };

    public CombatScenario Battle1 =>
        new()
        {
            Id = $"{SubgraphId}_{nameof(Battle1)}",
            Chapter = ChapterId,
            Subgraph = this,
            VictoryNode = $"{SubgraphId}_{nameof(Victory)}",
            DefeatNode = $"{SubgraphId}_{nameof(Defeat)}",
            EscapeNode = $"{SubgraphId}_{nameof(Escape)}",
            VictoryConditions = [],
            DefeatConditions = [new(CombatConditionKind.PlayerDefeated)],
            Background = new("backgrounds.harbor_district_dockside_dusk.png"),
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

    /// <summary>Introduces the harbor encounter and lets the player confront or avoid it.</summary>
    public NarrativeNode Intro =>
        new()
        {
            Id = $"{SubgraphId}_{nameof(Intro)}",
            Chapter = ChapterId,
            Subgraph = this,
            BackgroundImage = new("backgrounds.harbor_district_dockside_dusk.png"),
            ForegroundImage = new("characters.alley_thug_1.png"),
            RichText =
                "Fog rolls across the harbor. [b]Three figures block the dockside path.[/b]\nOne reaches for a weapon.",
            Choices =
            [
                new() { Text = "Confront the thugs", NextNode = Battle1.Id },
                new() { Text = "Leave the dockside", NextNode = $"{SubgraphId}_{nameof(Escape)}" },
            ],
        };

    /// <summary>Provides post-victory ability selection before the next encounter.</summary>
    public ProgressNode Progress1 =>
        new()
        {
            Id = $"{SubgraphId}_{nameof(Progress1)}",
            Chapter = ChapterId,
            Subgraph = this,
            NextNode = Progress2.Id,
        };

    /// <summary>Provides post-victory ability selection before the next encounter.</summary>
    public ProgressNode Progress2 =>
        new()
        {
            Id = $"{SubgraphId}_{nameof(Progress2)}",
            Chapter = ChapterId,
            Subgraph = this,
            NextNode = Progress3.Id,
        };

    /// <summary>Provides post-victory ability selection before the next encounter.</summary>
    public ProgressNode Progress3 =>
        new()
        {
            Id = $"{SubgraphId}_{nameof(Progress3)}",
            Chapter = ChapterId,
            Subgraph = this,
            NextNode = Progress4.Id,
        };

    /// <summary>Provides post-victory ability selection before the next encounter.</summary>
    public ProgressNode Progress4 =>
        new()
        {
            Id = $"{SubgraphId}_{nameof(Progress4)}",
            Chapter = ChapterId,
            Subgraph = this,
            NextNode = Victory.Id,
        };

    public OutcomeNode Victory =>
        new()
        {
            Id = $"{SubgraphId}_Victory",
            Chapter = ChapterId,
            Subgraph = this,
            Title = "Victory",
        };
    public OutcomeNode Defeat =>
        new()
        {
            Id = $"{SubgraphId}_Defeat",
            Chapter = ChapterId,
            Subgraph = this,
            Title = "Defeat",
        };
    public OutcomeNode Escape =>
        new()
        {
            Id = $"{SubgraphId}_Escape",
            Chapter = ChapterId,
            Subgraph = this,
            Title = "Escape",
        };

    public IEnumerable<CharacterData> Characters => [AlleywayThug1, AlleywayThug2, AlleywayThug3];

    public IEnumerable<StoryNode> Nodes =>
        [Intro, Battle1, Progress1, Progress2, Progress3, Progress4, Victory, Defeat, Escape];

    public StoryNodeId Start => Progress1.Id;

    public StoryNodeId End => Victory.Id;

    public Part1StoryGraph(IStoryGraph chapter)
    {
        Parent = chapter;
    }
}
