using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;
using NexusRealms.Prelude.Simulator;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var storyline = new Storyline();
var state = new SimulationState { CurrentStoryNodeId = storyline.StartNodeId.Value };
var startingLoadout = state.CreateGameState().Loadout;
Check(startingLoadout.Inventory.Single(item => item?.Id.Value == "starting-rocks")?.Quantity == 5,
    "Player must start with one inventory stack of five rocks.");
var first = SimulationRunner.Run(storyline, state, 42);
Check(first == SimulationRunner.Run(storyline, state, 42), "Seed replay or fresh-state isolation failed.");
Check(state.Health == 20, "Simulation mutated its starting state.");
Check(first.PlayerStrategy == state.PlayerStrategy.Strategy, "Run must record the configured player strategy.");
Check(first.EnemyConfiguration.Length == ((CombatScenario)storyline.Nodes[storyline.StartNodeId]).Characters.Length,
    "Run must record the scenario's character configuration.");
Check(first.PlayerActionUses.Values.Sum() > 0, "Run must record player action usage.");
Check(!string.IsNullOrWhiteSpace(first.TerminalCondition), "Run must record its terminal condition.");
var immediateDefeat = SimulationRunner.Run(storyline, state with { Health = 0 }, 42);
Check(immediateDefeat.Outcome == CombatOutcome.Defeat && immediateDefeat.TerminalCondition == "PlayerDefeated"
    && immediateDefeat.DecidingAction is null, "Initial defeat must be attributed to the player objective, not an action.");
var combat = new CombatSystem { TurnLimit = 100 };
var resolved = false;
combat.ScheduleEvent(100, "cutoff", _ => resolved = true);
combat.Process();
Check(combat.Outcome == CombatOutcome.Draw && !resolved, "An event at Turn 100 must not resolve.");
var before = new CombatSystem { TurnLimit = 100 };
before.ScheduleEvent(99.99f, "victory", system => system.End(CombatOutcome.Victory));
before.Process();
Check(before.Outcome == CombatOutcome.Victory, "Outcome before cutoff must count.");
var pump = new CombatSystem { TurnLimit = 100 };
pump.Add(new("player", true, 99));
pump.Add(new("enemy", false, 100));
pump.Process();
pump.SubmitAction(new(1, (_, _) => { }));
Check(pump.Outcome == CombatOutcome.Draw, "Automatic processing after an action crossed cutoff.");
var empty = (state with { Items = [], ItemLayout = [] }).CreateGameState();
Check(empty.Loadout.Items.Count == 0 && empty.Loadout.Equipment.Count == 0, "Empty saved loadout was not restored.");
var authored = (CombatScenario)storyline.Nodes[storyline.StartNodeId];
Check(authored.Characters.Select(placement => storyline.Characters[placement.CharacterId])
    .SelectMany(character => character.Actions)
    .All(action => action.BuildEffects().All(effect => effect is not RestoreEffect)),
    "Intro characters must not have a restoration action.");
var harmless = storyline.Characters.ToDictionary(pair => pair.Key, pair => pair.Value with
{
    Health = int.MaxValue,
    Actions = [new() { Damage = new DamageProfile() }],
});
var draw = SimulationRunner.Run(authored, harmless, state.CreateGameState(), 42);
Check(draw.Outcome == CombatOutcome.Draw && draw.Turn == 100, "Full encounter did not draw at Turn 100.");
Check(state.PlayerStrategy.Strategy == StrategyId.Aggressive, "Simulation must default to a named deterministic policy.");
Check(state.PlayerStrategies.Length == Enum.GetValues<StrategyId>().Length - 1
    && !state.PlayerStrategies.Contains(StrategyId.Protective), "Default strategy cycle must include each player-supported policy.");
var limitedState = state with { PlayerStrategies = [StrategyId.Analytical, StrategyId.Cautious] };
Check(limitedState.PlayerStrategyForRun(0).Strategy == StrategyId.Analytical
    && limitedState.PlayerStrategyForRun(1).Strategy == StrategyId.Cautious
    && limitedState.PlayerStrategyForRun(2).Strategy == StrategyId.Analytical,
    "Configured player strategies must cycle in order.");
try
{
    _ = (state with { PlayerStrategies = [StrategyId.Protective] }).PlayerStrategyForRun(0);
    throw new Exception("Player strategy cycle accepted Protective without player allies.");
}
catch (ArgumentException) { }
Check(draw.TerminalCondition == "TurnLimit" && draw.DecidingAction is null, "Turn-limit draws must not be attributed to a combat action.");
var reportSamples = new[]
{
    first with
    {
        PlayerStrategy = StrategyId.Analytical,
        Outcome = CombatOutcome.Victory,
        TerminalCondition = "AllEnemiesDefeated",
        DecidingAction = "Throw [starting-rocks]",
        DecidingActionSource = "Player",
        PlayerActionUses = new Dictionary<string, int> { ["Throw [starting-rocks]"] = 3 },
    },
    first with
    {
        PlayerStrategy = StrategyId.Analytical,
        Outcome = CombatOutcome.Defeat,
        TerminalCondition = "PlayerDefeated",
        DecidingAction = "strike",
        DecidingActionSource = "Enemy",
        PlayerActionUses = new Dictionary<string, int> { ["Throw [starting-rocks]"] = 1 },
    },
};
var report = SimulationReport.Format(reportSamples);
Check(report.Contains("Analytical\t1\t1\t0\t0\t50.00", StringComparison.Ordinal)
    && report.Contains("Throw [starting-rocks]\tVictory\t0\t0\t0\t1\t0", StringComparison.Ordinal)
    && report.Contains("Player strategy x enemy configuration x deciding action:", StringComparison.Ordinal),
    "Simulation report must include strategy rates, usage distributions, and strategy/action cross-tabs.");
// Complete item-specific Throw choices are covered by AiDecisionChecks.
try
{
    SimulationRunner.Run(storyline, state with { CurrentStoryNodeId = "missing" }, 42);
    throw new Exception("Missing scenario was accepted.");
}
catch (ArgumentException) { }
Console.WriteLine("Simulation checks passed.");
