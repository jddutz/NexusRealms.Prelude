using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Simulator;

/// <summary>A saved starting state; the story node selects the authored encounter.</summary>
public sealed record SimulationState
{
    public required string CurrentStoryNodeId { get; init; }
    public int Health { get; init; } = 20;
    public int Focus { get; init; } = 2;
    public string[] StatusEffects { get; init; } = ["guarded", "inspired", "focused"];
    // Omit both to use Prelude's starting inventory. Empty collections mean unarmed.
    public CarriedItem[]? Items { get; init; }
    public Dictionary<string, string>? ItemLayout { get; init; }
    public bool AllowEscape { get; init; }

    public GameState CreateGameState()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Health);
        ArgumentOutOfRangeException.ThrowIfNegative(Focus);
        if (string.IsNullOrWhiteSpace(CurrentStoryNodeId)) throw new ArgumentException("CurrentStoryNodeId is required.");
        if ((Items is null) != (ItemLayout is null)) throw new ArgumentException("Items and ItemLayout must be supplied together.");
        var loadout = Items is null ? CombatLoadout.CreatePlayerLoadout() : CombatLoadout.FromState(Items, ItemLayout!, Focus);
        loadout.Focus = Focus;
        return new(CurrentStoryNodeId) { Health = Health, Loadout = loadout, StatusEffects = StatusEffects.Select(id => new StatusEffectId(id)).ToArray() };
    }
}
