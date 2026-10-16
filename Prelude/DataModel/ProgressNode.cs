namespace NexusRealms.Prelude.DataModel;

/// <summary>A story visit to the player's Zodiac progression scene.</summary>
public sealed record ProgressNode : StoryNode
{
    public required int Chapter { get; init; }
    public required StoryNodeId NextNode { get; init; }
}