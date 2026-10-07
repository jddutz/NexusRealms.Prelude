using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Provides a base implementation of <see cref="IStoryGraph"/> for defining story segments.
/// </summary>
public abstract record StoryGraph : IStoryGraph
{
    /// <summary>
    /// Gets or initializes the characters made available to this graph's nodes.
    /// </summary>
    public ImmutableArray<Character> Characters { get; init; } = [];

    /// <summary>
    /// Gets or initializes the story nodes contained in this graph.
    /// </summary>
    public ImmutableArray<StoryNode> Nodes { get; init; } = [];

    /// <summary>
    /// Gets or initializes the identifier of the node where this graph begins.
    /// </summary>
    public StoryNodeId Start { get; init; } = StoryNodeId.Invalid;

    /// <summary>
    /// Gets or initializes the identifier of the node where this graph ends.
    /// </summary>
    public StoryNodeId End { get; init; } = StoryNodeId.Invalid;
}
