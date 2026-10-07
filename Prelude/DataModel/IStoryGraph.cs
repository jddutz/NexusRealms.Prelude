namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines a story segment, including its characters, nodes, and entry and exit points.
/// </summary>
public interface IStoryGraph
{
    /// <summary>
    /// Gets the characters made available to the nodes in this graph.
    /// </summary>
    public ImmutableArray<Character> Characters { get; }

    /// <summary>
    /// Gets the story nodes contained in this graph.
    /// </summary>
    public ImmutableArray<StoryNode> Nodes { get; }

    /// <summary>
    /// Gets the identifier of the node where this graph begins.
    /// </summary>
    public StoryNodeId Start { get; }

    /// <summary>
    /// Gets the identifier of the node where this graph ends, or
    /// <see cref="StoryNodeId.Invalid"/> when no end node is defined.
    /// </summary>
    public StoryNodeId End { get; }
}
