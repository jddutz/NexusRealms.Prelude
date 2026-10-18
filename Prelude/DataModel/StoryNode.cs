namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines a node in a story graph.
/// </summary>
public abstract record StoryNode
{
    /// <summary>
    /// Gets the node's unique identifier.
    /// </summary>
    public required StoryNodeId Id { get; init; }
    public required int Chapter { get; init; }
    public required IStoryGraph Subgraph { get; init; }
}
