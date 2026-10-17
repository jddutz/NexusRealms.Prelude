namespace NexusRealms.Prelude.DataModel;

/// <summary>A placeholder story destination for an encounter outcome.</summary>
public sealed record OutcomeNode : StoryNode
{
    /// <summary>Gets the optional node to load when the outcome is continued.</summary>
    public StoryNodeId? NextNode { get; init; }

    /// <summary>Gets the title displayed for this outcome.</summary>
    public required string Title { get; init; }
}
