namespace NexusRealms.Prelude.DataModel;

public record GameState(StoryNodeId CurrentStoryNodeId)
{
    /// <summary>Gets the player's portrait texture.</summary>
    public ContentId Portrait { get; init; } = new("portraits.mercenary_male.png");
    public int Initiative { get; init; }
    public int Health { get; init; } = 5;
    public int Focus { get; init; } = 2;
}

