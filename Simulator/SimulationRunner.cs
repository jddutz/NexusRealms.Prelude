using NexusRealms.Prelude;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;
using NexusRealms.Prelude.Actions;

namespace NexusRealms.Prelude.Simulator;

/// <summary>A character's authored simulation configuration for one scenario run.</summary>
/// <param name="CharacterId">The character definition identifier.</param>
/// <param name="Strategy">The character's configured decision strategy.</param>
/// <param name="Team">The character's combat team.</param>
/// <param name="Slot">The character's initial formation slot.</param>
/// <param name="InitialTurn">The character's initial timeline turn.</param>
/// <param name="Initiative">The character's same-turn ordering priority.</param>
/// <param name="RequiredForVictory">Whether this character is a required victory objective.</param>
/// <param name="Health">The character's starting Health.</param>
/// <param name="Focus">The character's starting Focus.</param>
/// <param name="ActionIds">The character's available action identifiers.</param>
public sealed record SimulationEnemyConfiguration(string CharacterId, StrategyId Strategy, CombatTeam Team,
    FormationSlot Slot, float InitialTurn, int Initiative, bool RequiredForVictory, int Health, int Focus,
    string[] ActionIds);

/// <summary>One completed combat run and the dimensions used by the simulation report.</summary>
/// <param name="Seed">The run's replay seed.</param>
/// <param name="Outcome">The encounter outcome.</param>
/// <param name="Turn">The ending timeline turn.</param>
/// <param name="PlayerHealth">The player's remaining Health.</param>
/// <param name="Actions">The number of simulation-loop operations.</param>
public sealed record SimulationResult(int Seed, CombatOutcome Outcome, float Turn, int PlayerHealth, int Actions)
{
    /// <summary>The configured base strategy for the simulated player.</summary>
    public StrategyId PlayerStrategy { get; init; } = StrategyId.Aggressive;

    /// <summary>The placed non-player characters and their authored configuration.</summary>
    public SimulationEnemyConfiguration[] EnemyConfiguration { get; init; } = [];

    /// <summary>The objective or boundary which ended the encounter.</summary>
    public string TerminalCondition { get; init; } = "";

    /// <summary>The action whose resolution reached the terminal condition, when one did.</summary>
    public string? DecidingAction { get; init; }

    /// <summary>The display name of the actor that performed the deciding action, when present.</summary>
    public string? DecidingActionSource { get; init; }

    /// <summary>Counts of each action selected by the simulated player.</summary>
    public IReadOnlyDictionary<string, int> PlayerActionUses { get; init; } =
        new SortedDictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Compares run values and report dimensions independent of collection instances.</summary>
    /// <param name="other">The result to compare with this result.</param>
    /// <returns>True when all run and report values match.</returns>
    public bool Equals(SimulationResult? other) =>
        other is not null
        && Seed == other.Seed && Outcome == other.Outcome && Turn == other.Turn
        && PlayerHealth == other.PlayerHealth && Actions == other.Actions
        && PlayerStrategy == other.PlayerStrategy && TerminalCondition == other.TerminalCondition
        && DecidingAction == other.DecidingAction && DecidingActionSource == other.DecidingActionSource
        && EnemyConfiguration.Length == other.EnemyConfiguration.Length
        && EnemyConfiguration.Zip(other.EnemyConfiguration).All(pair => SameConfiguration(pair.First, pair.Second))
        && PlayerActionUses.Count == other.PlayerActionUses.Count
        && PlayerActionUses.All(pair => other.PlayerActionUses.TryGetValue(pair.Key, out var value) && pair.Value == value);

    /// <summary>Creates a hash consistent with value equality for the report dimensions.</summary>
    /// <returns>The hash code for this simulation result.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Seed);
        hash.Add(Outcome);
        hash.Add(Turn);
        hash.Add(PlayerHealth);
        hash.Add(Actions);
        hash.Add(PlayerStrategy);
        hash.Add(TerminalCondition, StringComparer.Ordinal);
        hash.Add(DecidingAction, StringComparer.Ordinal);
        hash.Add(DecidingActionSource, StringComparer.Ordinal);
        foreach (var enemy in EnemyConfiguration) AddConfigurationHash(ref hash, enemy);
        foreach (var action in PlayerActionUses.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            hash.Add(action.Key, StringComparer.Ordinal);
            hash.Add(action.Value);
        }
        return hash.ToHashCode();
    }

    /// <summary>Compares enemy configuration data including the available action list.</summary>
    /// <param name="left">The first character configuration.</param>
    /// <param name="right">The second character configuration.</param>
    /// <returns>True when all character configuration values match.</returns>
    private static bool SameConfiguration(SimulationEnemyConfiguration left, SimulationEnemyConfiguration right) =>
        left.CharacterId == right.CharacterId && left.Strategy == right.Strategy && left.Team == right.Team
        && left.Slot == right.Slot && left.InitialTurn == right.InitialTurn && left.Initiative == right.Initiative
        && left.RequiredForVictory == right.RequiredForVictory && left.Health == right.Health && left.Focus == right.Focus
        && left.ActionIds.SequenceEqual(right.ActionIds, StringComparer.Ordinal);

    /// <summary>Adds an enemy configuration's values to a run-result hash.</summary>
    /// <param name="hash">The hash builder being populated.</param>
    /// <param name="enemy">The enemy configuration to add.</param>
    private static void AddConfigurationHash(ref HashCode hash, SimulationEnemyConfiguration enemy)
    {
        hash.Add(enemy.CharacterId, StringComparer.Ordinal);
        hash.Add(enemy.Strategy);
        hash.Add(enemy.Team);
        hash.Add(enemy.Slot);
        hash.Add(enemy.InitialTurn);
        hash.Add(enemy.Initiative);
        hash.Add(enemy.RequiredForVictory);
        hash.Add(enemy.Health);
        hash.Add(enemy.Focus);
        foreach (var actionId in enemy.ActionIds) hash.Add(actionId, StringComparer.Ordinal);
    }
}

/// <summary>Executes headless Prelude combat and captures per-run report dimensions.</summary>
public static class SimulationRunner
{
    /// <summary>The exclusive timeline boundary for simulated encounters.</summary>
    public const float TurnLimit = 100f;

    /// <summary>Runs the combat scenario selected by the saved story node.</summary>
    /// <param name="storyline">The authored story database.</param>
    /// <param name="initial">The saved simulation starting state.</param>
    /// <param name="seed">The deterministic run seed.</param>
    /// <returns>The completed run and its report dimensions.</returns>
    public static SimulationResult Run(Storyline storyline, SimulationState initial, int seed)
    {
        var state = initial.CreateGameState();
        if (!storyline.Nodes.TryGetValue(state.CurrentStoryNodeId, out var node) || node is not CombatScenario scenario)
            throw new ArgumentException($"'{state.CurrentStoryNodeId}' is not an authored combat scenario.");
        return Run(scenario, storyline.Characters, state, seed, initial.AllowEscape, initial.PlayerStrategy);
    }

    /// <summary>Runs a supplied combat scenario using Prelude's shared resolver and decision policies.</summary>
    /// <param name="scenario">The authored combat scenario.</param>
    /// <param name="characters">Character definitions referenced by the scenario.</param>
    /// <param name="state">The fresh mutable game state for this run.</param>
    /// <param name="seed">The deterministic run seed.</param>
    /// <param name="allowEscape">Whether the simulated player may escape.</param>
    /// <param name="playerStrategy">Optional player policy, defaulting to the aggressive policy.</param>
    /// <param name="trace">Optional callback for each completed AI decision.</param>
    /// <returns>The completed run and its report dimensions.</returns>
    public static SimulationResult Run(CombatScenario scenario, IReadOnlyDictionary<CharacterId, CharacterData> characters,
        GameState state, int seed, bool allowEscape = false, StrategyAssignment? playerStrategy = null,
        Action<DecisionTrace>? trace = null)
    {
        var combat = new CombatSystem(unchecked((uint)seed)) { TurnLimit = TurnLimit };
        var health = new Dictionary<string, DamageReceiver> { ["player"] = new(state.Health) };
        var resolver = new CombatActionResolver(combat, Database.StatusEffects.All) { AllowEscape = allowEscape, CaptureCandidateTraces = trace is not null };
        foreach (var ability in Database.Abilities.All.Values) resolver.RegisterAbility(ability);
        resolver.Add(new("player", true, CombatTeam.PlayerAndAllies, health["player"], state.Loadout), state.StatusEffects);
        combat.Add(new("player", true, initiative: scenario.PlayerInitiative));
        var definitions = new Dictionary<string, CharacterData>();
        var enemyConfiguration = new List<SimulationEnemyConfiguration>();
        for (var index = 0; index < scenario.Characters.Length; index++)
        {
            var placement = scenario.Characters[index];
            var id = $"encounter-{index}";
            var definition = characters[placement.CharacterId];
            health[id] = new(definition.Health);
            definitions[id] = definition;
            enemyConfiguration.Add(new(placement.CharacterId.Value, definition.Strategy.Strategy, placement.Team,
                placement.Slot, placement.InitialTurn, placement.Initiative, placement.RequiredForVictory,
                definition.Health, definition.Focus, definition.Actions.Select(action => action.Id).ToArray()));
            combat.Add(new(id, false, placement.InitialTurn, placement.Initiative, placement.Team, Row(placement.Slot)));
            resolver.Add(new(id, false, placement.Team, health[id], CombatLoadout.FromCharacter(definition), placement.Slot), definition.StatusEffects);
        }
        combat.IsCombatantAlive = id => health.TryGetValue(id, out var receiver) && receiver.Health > 0;
        foreach (var (id, receiver) in health)
        {
            if (id == "player") receiver.HealthChanged += () => state.Health = receiver.Health;
            receiver.DamageTaken += result => { if (result.IsLethal) combat.Remove(id); };
            if (receiver.Health == 0) combat.Remove(id);
        }
        scenario.ConfigureCombat(combat);
        foreach (var occurrence in scenario.Events) resolver.ScheduleEvent(occurrence);
        resolver.ConfigureCharacters(definitions);
        var assignment = playerStrategy ?? new();
        resolver.Behavior.Assign("player", assignment);
        var actionUses = new SortedDictionary<string, int>(StringComparer.Ordinal);
        Action<DecisionTrace> captureDecision = decision =>
        {
            if (decision.ActorId == "player" && decision.Selected is { } selected)
            {
                var actionId = ActionLabel(selected.Action.Id, selected.ItemId);
                actionUses[actionId] = actionUses.GetValueOrDefault(actionId) + 1;
            }
            trace?.Invoke(decision);
        };
        ActionOutcome? resolvedAction = null;
        ActionOutcome? committedAction = null;
        string? committingActorId = null;
        string? committedActorId = null;
        string? terminalCondition = null;
        string? decidingAction = null;
        string? decidingActionSource = null;
        resolver.Resolved += outcome => resolvedAction = outcome;
        combat.ActionCommitting += actorId =>
        {
            resolvedAction = null;
            committedAction = null;
            committedActorId = null;
            committingActorId = actorId;
        };
        combat.ActionCommitted += actorId =>
        {
            committedAction = resolvedAction;
            committedActorId = actorId;
            resolvedAction = null;
            committingActorId = null;
        };
        combat.Ended += outcome =>
        {
            terminalCondition = TerminalCondition(outcome, scenario, combat, definitions);
            if (outcome == CombatOutcome.Draw) return;
            var decisive = resolvedAction;
            if (decisive is null && outcome == CombatOutcome.Escape && committedActorId == "player")
                decisive = committedAction;
            if (decisive is null) return;
            var candidate = committingActorId == decisive.SourceId
                && resolver.LastDecision is { } decision && decision.ActorId == decisive.SourceId
                ? decision.Selected : null;
            decidingAction = ActionLabel(decisive.Action.Id, candidate?.Action.Id == decisive.Action.Id ? candidate.ItemId : null);
            decidingActionSource = decisive.SourceId == "player"
                ? "Player"
                : definitions.GetValueOrDefault(decisive.SourceId)?.Name ?? decisive.SourceId;
        };
        combat.EventResolved += () =>
        {
            combat.EvaluateOutcome();
            resolvedAction = null;
            committedAction = null;
            committedActorId = null;
        };
        var actions = 0;
        combat.Process();
        while (!combat.HasEnded)
        {
            if (++actions > 1_000_000) throw new InvalidOperationException("Encounter exceeded the safety operation budget.");
            if (combat.ActiveCombatant is not { } actor)
            {
                if (combat.Timeline.Preview(1).Count == 0) throw new InvalidOperationException("Encounter has no pending occurrences or outcome.");
                combat.Process();
                continue;
            }
            var decision = CombatDecisionMaker.DecideAndCommit(resolver, actor.Id);
            captureDecision(decision);
        }
        state.LastCombatOutcome = combat.Outcome;
        return new(seed, combat.Outcome ?? throw new InvalidOperationException("Encounter ended without an outcome."),
            combat.Outcome == CombatOutcome.Draw ? TurnLimit : combat.Timeline.CurrentTurn, state.Health, actions)
        {
            PlayerStrategy = assignment.Strategy,
            EnemyConfiguration = enemyConfiguration.ToArray(),
            TerminalCondition = terminalCondition ?? combat.Outcome.Value.ToString(),
            DecidingAction = decidingAction,
            DecidingActionSource = decidingActionSource,
            PlayerActionUses = actionUses,
        };
    }

    /// <summary>Creates a stable action label, retaining the chosen item for throws.</summary>
    /// <param name="actionId">The resolved action identifier.</param>
    /// <param name="itemId">The selected item identifier, if the action used a specific item.</param>
    /// <returns>A report label for the action.</returns>
    private static string ActionLabel(string actionId, string? itemId) =>
        itemId is null ? actionId : $"{actionId} [{itemId}]";

    /// <summary>Identifies the authored objective or boundary that ended the encounter.</summary>
    /// <param name="outcome">The resolved encounter outcome.</param>
    /// <param name="scenario">The authored scenario.</param>
    /// <param name="combat">The completed combat state.</param>
    /// <param name="definitions">The character definitions indexed by runtime identity.</param>
    /// <returns>A stable label for the satisfied terminal condition.</returns>
    private static string TerminalCondition(CombatOutcome outcome, CombatScenario scenario, CombatSystem combat,
        IReadOnlyDictionary<string, CharacterData> definitions)
    {
        if (outcome == CombatOutcome.Draw) return "TurnLimit";
        if (outcome == CombatOutcome.Escape) return "Escape";
        var conditions = outcome == CombatOutcome.Defeat
            ? scenario.DefeatConditions
            : scenario.VictoryConditions.Concat(scenario.Characters.Select((placement, index) => (placement, index))
                .Where(entry => entry.placement.RequiredForVictory)
                .Select(entry => new CombatConditionData(CombatConditionKind.CharacterDefeated, $"encounter-{entry.index}")))
                .DefaultIfEmpty(new(CombatConditionKind.AllEnemiesDefeated));
        var satisfied = conditions.Where(condition => condition.Kind switch
        {
            CombatConditionKind.PlayerDefeated => !combat.IsAlive("player"),
            CombatConditionKind.AllEnemiesDefeated => !combat.HasLivingEnemies,
            CombatConditionKind.CharacterDefeated => !combat.IsAlive(condition.CharacterId ?? ""),
            _ => false,
        }).Select(condition =>
        {
            if (condition.Kind != CombatConditionKind.CharacterDefeated) return condition.Kind.ToString();
            var id = condition.CharacterId ?? "";
            return definitions.TryGetValue(id, out var character)
                ? $"{condition.Kind}:{character.Name}" : $"{condition.Kind}:{id}";
        }).ToArray();
        return satisfied.Length == 0 ? outcome.ToString() : string.Join("+", satisfied);
    }

    /// <summary>Maps an authored formation position to its scheduler row.</summary>
    /// <param name="slot">The character's formation slot.</param>
    /// <returns>The corresponding combat timeline row.</returns>
    private static CombatRow Row(FormationSlot slot) => slot >= FormationSlot.FrontLeft ? CombatRow.Front
        : slot >= FormationSlot.MiddleLeft ? CombatRow.Middle : CombatRow.Back;
}
