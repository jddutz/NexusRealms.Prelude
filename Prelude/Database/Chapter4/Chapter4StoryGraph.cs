namespace NexusRealms.Prelude.Database.Chapter4;

/// <summary>
/// Provides the story graph for Chapter 4.
/// </summary>
public sealed record Chapter4StoryGraph : IStoryGraph
{
    public IStoryGraph? Parent { get; init; }

    public const int ChapterId = 4;
    public static readonly string SubgraphId = $"ch{ChapterId}";

    public IStoryGraph Part1 { get; init; }

    public IEnumerable<CharacterData> Characters => [.. Part1.Characters];

    public IEnumerable<StoryNode> Nodes => [.. Part1.Nodes];

    public StoryNodeId Start => Part1.Start;

    public StoryNodeId End => Part1.End;

    public Chapter4StoryGraph(IStoryGraph storyline)
    {
        Part1 = new Part1StoryGraph(this);
        Parent = storyline;
    }
}
