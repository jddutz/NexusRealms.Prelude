namespace NexusRealms.Prelude.DataModel;

public record GameState(StoryNodeId CurrentStoryNodeId)
{
    /// <summary>Gets the player's portrait texture.</summary>
    public ContentId Portrait { get; init; } = new("portraits.mercenary_male.png");
    public Combat.CombatLoadout Loadout { get; } = Combat.CombatLoadout.CreatePlayerLoadout();
    /// <summary>Active status effect identifiers shared by the HUD and Character dialog.</summary>
    public StatusEffectId[] StatusEffects { get; init; } = ["guarded", "inspired", "focused"];
    public int Health { get; init; } = 5;
    public int Focus { get => Loadout.Focus; init => Loadout.Focus = value; }
}

