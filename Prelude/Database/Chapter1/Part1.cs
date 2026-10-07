namespace NexusRealms.Prelude.Database.Chapter1;

/// <summary>
/// Provides the story graph for the first part of Chapter 1.
/// </summary>
public sealed record Part1 : StoryGraph
{
    public const string SubgraphId = "ch1_p1";

    public static readonly Character AlleywayThug1 = new()
    {
        Id = new($"{SubgraphId}_{nameof(AlleywayThug1)}"),
        Name = "Alleyway Thug #1",
        Artwork = new("alley_thug_1"),
    };

    public static readonly Character AlleywayThug2 = new()
    {
        Id = new($"{SubgraphId}_{nameof(AlleywayThug2)}"),
        Name = "Alleyway Thug #2",
        Artwork = new("alley_thug_2"),
    };

    public static readonly Character AlleywayThug3 = new()
    {
        Id = new($"{SubgraphId}_{nameof(AlleywayThug3)}"),
        Name = "Alleyway Thug #3",
        Artwork = new("alley_thug_3"),
    };

    public static readonly StoryNode Intro = new CombatScenario
    {
        Id = $"{SubgraphId}_{nameof(Intro)}",
        Background = new("alley_1"),
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
            },
            new CharacterPlacement
            {
                CharacterId = AlleywayThug3.Id,
                Slot = FormationSlot.MiddleRight,
            },
        ],
    };

    /// <summary>
    /// Creates the first part of Chapter 1.
    /// </summary>
    public Part1()
    {
        Characters = [AlleywayThug1, AlleywayThug2, AlleywayThug3];

        Nodes = [Intro];

        Start = Intro.Id;
        End = StoryNodeId.Invalid;
    }
}
