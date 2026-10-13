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
    /// <summary>Base policy settings for the simulated player; normal gameplay still uses player input.</summary>
    public StrategyAssignment PlayerStrategy { get; init; } = new();
    /// <summary>Strategies cycled by the simulator CLI; Protective is excluded because the player has no allies.</summary>
    public StrategyId[] PlayerStrategies { get; init; } =
    [
        StrategyId.Aggressive, StrategyId.Disruptive, StrategyId.Opportunistic, StrategyId.Chaotic,
        StrategyId.Reactive, StrategyId.Analytical, StrategyId.Defensive, StrategyId.Supportive,
        StrategyId.Cautious, StrategyId.Reckless,
    ];
    /// <summary>Learned progression nodes whose authored actions are available, subject to equipment requirements.</summary>
    public string[] LearnedAbilities { get; init; } = [];

    /// <summary>Selects the configured player policy for a zero-based simulation run.</summary>
    /// <param name="runIndex">The zero-based run index.</param>
    /// <returns>The base policy settings with the selected strategy.</returns>
    public StrategyAssignment PlayerStrategyForRun(int runIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(runIndex);
        if (PlayerStrategies is null || PlayerStrategies.Length == 0)
            throw new ArgumentException("At least one player strategy must be configured.", nameof(PlayerStrategies));
        if (PlayerStrategies.Contains(StrategyId.Protective))
            throw new ArgumentException("Protective is not supported for the player because the player has no allies.", nameof(PlayerStrategies));
        if (PlayerStrategies.Distinct().Count() != PlayerStrategies.Length)
            throw new ArgumentException("Player strategies must not contain duplicates.", nameof(PlayerStrategies));
        foreach (var strategy in PlayerStrategies) (PlayerStrategy with { Strategy = strategy }).Validate();
        return PlayerStrategy with { Strategy = PlayerStrategies[runIndex % PlayerStrategies.Length] };
    }

    public GameState CreateGameState()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Health);
        ArgumentOutOfRangeException.ThrowIfNegative(Focus);
        if (string.IsNullOrWhiteSpace(CurrentStoryNodeId)) throw new ArgumentException("CurrentStoryNodeId is required.");
        if ((Items is null) != (ItemLayout is null)) throw new ArgumentException("Items and ItemLayout must be supplied together.");
        var loadout = Items is null ? CombatLoadout.CreatePlayerLoadout() : CombatLoadout.FromState(Items, ItemLayout!, Focus);
        loadout.Focus = Focus;
        foreach (var id in LearnedAbilities)
        {
            if (!Database.Abilities.All.TryGetValue(new AbilityId(id), out var ability)) throw new ArgumentException($"Unknown learned ability '{id}'.");
            loadout.Learn(ability);
        }
        return new(CurrentStoryNodeId) { Health = Health, Loadout = loadout, StatusEffects = StatusEffects.Select(id => new StatusEffectId(id)).ToArray() };
    }
}
