using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Simulator;

/// <summary>A saved starting state; the story node selects the authored encounter.</summary>
public sealed record SimulationState
{
    /// <summary>The authored combat scenario to simulate.</summary>
    public required string CurrentStoryNodeId { get; init; }
    /// <summary>The player's starting Health.</summary>
    public int Health { get; init; } = 20;
    /// <summary>The player's starting Focus.</summary>
    public int Focus { get; init; } = 2;
    /// <summary>The player's starting status effect identifiers.</summary>
    public string[] StatusEffects { get; init; } = ["guarded", "inspired", "focused"];
    // Omit both to use Prelude's starting inventory. Empty collections mean unarmed.
    /// <summary>The player's starting carried items, or null to use the authored starting inventory.</summary>
    public CarriedItem[]? Items { get; init; }
    /// <summary>The player's starting item layout; must be supplied with <see cref="Items"/>.</summary>
    public Dictionary<string, string>? ItemLayout { get; init; }
    /// <summary>Whether the simulated player is allowed to escape.</summary>
    public bool AllowEscape { get; init; }
    /// <summary>The zodiac applied to the player in this saved starting state.</summary>
    public ZodiacSignId Zodiac { get; init; } = ZodiacSignId.Aquarius;
    /// <summary>Base policy settings for the simulated player; normal gameplay still uses player input.</summary>
    public StrategyAssignment PlayerStrategy { get; init; } = new();
    /// <summary>Zodiac signs cycled by the simulator CLI.</summary>
    public ZodiacSignId[] PlayerZodiacs { get; init; } = Enum.GetValues<ZodiacSignId>();
    /// <summary>Strategies cycled by the simulator CLI; Protective is excluded because the player has no allies.</summary>
    public StrategyId[] PlayerStrategies { get; init; } =
    [
        StrategyId.Aggressive, StrategyId.Disruptive, StrategyId.Opportunistic, StrategyId.Chaotic,
        StrategyId.Reactive, StrategyId.Analytical, StrategyId.Defensive, StrategyId.Supportive,
        StrategyId.Cautious, StrategyId.Reckless,
    ];
    /// <summary>Additional ability IDs resolved within the selected zodiac graph.</summary>
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

    /// <summary>Selects the configured zodiac for a run, holding each sign for one strategy cycle.</summary>
    /// <param name="runIndex">The zero-based run index.</param>
    /// <returns>The configured zodiac sign for the run.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is negative.</exception>
    /// <exception cref="ArgumentException">No strategies or zodiacs are configured, zodiacs repeat, or a sign is invalid.</exception>
    public ZodiacSignId PlayerZodiacForRun(int runIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(runIndex);
        var strategyCount = PlayerStrategies?.Length ?? 0;
        if (strategyCount == 0)
            throw new ArgumentException("At least one player strategy must be configured.", nameof(PlayerStrategies));
        if (PlayerZodiacs is null || PlayerZodiacs.Length == 0)
            throw new ArgumentException("At least one player zodiac must be configured.", nameof(PlayerZodiacs));
        if (PlayerZodiacs.Distinct().Count() != PlayerZodiacs.Length)
            throw new ArgumentException("Player zodiacs must not contain duplicates.", nameof(PlayerZodiacs));
        foreach (var zodiac in PlayerZodiacs)
            if (!Enum.IsDefined(zodiac))
                throw new ArgumentException($"Unknown player zodiac '{zodiac}'.", nameof(PlayerZodiacs));
        return PlayerZodiacs[(runIndex / strategyCount) % PlayerZodiacs.Length];
    }

    /// <summary>Creates a fresh runtime state from the saved simulation configuration.</summary>
    /// <returns>The mutable runtime state used to begin one simulation.</returns>
    public GameState CreateGameState()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Health);
        ArgumentOutOfRangeException.ThrowIfNegative(Focus);
        if (string.IsNullOrWhiteSpace(CurrentStoryNodeId)) throw new ArgumentException("CurrentStoryNodeId is required.");
        if ((Items is null) != (ItemLayout is null)) throw new ArgumentException("Items and ItemLayout must be supplied together.");
        if (!Database.Progression.ProgressionGraphs.All.TryGetValue(Zodiac, out var graph))
            throw new ArgumentException($"No progression graph is defined for zodiac '{Zodiac}'.", nameof(Zodiac));
        var loadout = Items is null ? CombatLoadout.CreatePlayerLoadout() : CombatLoadout.FromState(Items, ItemLayout!, Focus);
        loadout.Focus = Focus;
        var abilityDefinitions = graph.AbilityDefinitionsById;
        foreach (var id in graph.Abilities.Select(node => node.Id.Value).Concat(LearnedAbilities).Distinct(StringComparer.Ordinal))
        {
            if (!abilityDefinitions.TryGetValue(new AbilityId(id), out var ability)) throw new ArgumentException($"Unknown learned ability '{id}' for zodiac '{Zodiac}'.");
            loadout.Learn(ability);
        }
        return new(CurrentStoryNodeId)
        {
            Zodiac = Zodiac,
            Health = Health,
            Loadout = loadout,
            StatusEffects = StatusEffects.Select(id => new StatusEffectId(id)).ToArray(),
        };
    }
}
