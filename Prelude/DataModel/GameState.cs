namespace NexusRealms.Prelude.DataModel;

public record GameState(StoryNodeId CurrentStoryNodeId)
{
    /// <summary>Gets the player's portrait texture.</summary>
    public ContentId Portrait { get; init; } = new("portraits.mercenary_male.png");
    public Combat.CombatLoadout Loadout { get; } = CreateLoadout();
    private static Combat.CombatLoadout CreateLoadout()
    {
        var loadout = new Combat.CombatLoadout { Focus = 2 };
        loadout.Abilities.Add(new("wait", "Wait", 0.6f, RequiresTarget: false, Icon: "icons.status_icons.png", IconRegion: "moon"));
        loadout.Assign(0, "wait");
        return loadout;
    }
    public int Initiative { get; init; }
    public int Health { get; init; } = 5;
    public int Focus { get => Loadout.Focus; init => Loadout.Focus = value; }
}

