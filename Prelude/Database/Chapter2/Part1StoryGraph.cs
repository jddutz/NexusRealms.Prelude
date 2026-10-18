namespace NexusRealms.Prelude.Database.Chapter2;

/// <summary>
/// Defines the story graph for the first part of Chapter 5.
/// </summary>
public sealed record Part1StoryGraph : IStoryGraph
{
    public static readonly int ChapterId = Chapter2StoryGraph.ChapterId;
    public static readonly string SubgraphId = $"{Chapter2StoryGraph.SubgraphId}_p1";

    public IStoryGraph? Parent { get; init; }

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

    public IEnumerable<CharacterData> Characters => [];

    public IEnumerable<StoryNode> Nodes => [Progress1, Progress2, Progress3, Progress4, Victory];

    public StoryNodeId Start => Progress1.Id;

    public StoryNodeId End => Victory.Id;

    public Part1StoryGraph(IStoryGraph chapter)
    {
        Parent = chapter;
    }
}
