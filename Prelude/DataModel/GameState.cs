namespace NexusRealms.Prelude.DataModel;

public record GameState(StoryNodeId CurrentStoryNodeId)
{
    /// <summary>Gets the player's portrait texture.</summary>
    public ContentId Portrait { get; init; } = new("portraits.mercenary_male.png");
    public int Health { get; init; } = 80;
    public int MaximumHealth { get; init; } = 100;
    public int Focus { get; init; } = 45;
    public int MaximumFocus { get; init; } = 60;
}
