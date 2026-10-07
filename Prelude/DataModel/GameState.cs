namespace NexusRealms.Prelude.DataModel;

public record GameState(StoryNodeId CurrentStoryNodeId)
{
    /// <summary>Gets the player's portrait texture.</summary>
    public ContentId Portrait { get; init; } = new("portraits.mercenary_male.png");
}
