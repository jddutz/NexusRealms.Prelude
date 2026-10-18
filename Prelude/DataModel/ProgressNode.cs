namespace NexusRealms.Prelude.DataModel;

/// <summary>A story visit to the player's Zodiac progression scene.</summary>
public sealed record ProgressNode : StoryNode
{
    public required StoryNodeId NextNode { get; init; }
}