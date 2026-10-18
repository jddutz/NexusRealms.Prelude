using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;
using NexusRealms.Prelude.Simulator;
using System.Text.Json;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var storyline = new Storyline();
var state = new SimulationState { CurrentStoryNodeId = NexusRealms.Prelude.Database.Chapter1.Part1.Battle1.Id.Value };
var aquariusState = state with { LearnedAbilities = ["Heal", "Bewilder"] };
var aquariusLoadout = aquariusState.CreateGameState().Loadout;
var aquariusGraph = NexusRealms.Prelude.Database.Progression.ProgressionGraphs.All[ZodiacSignId.Aquarius];
Check(aquariusLoadout.LearnedAbilities.SetEquals(aquariusGraph.Abilities.Select(node => node.Id))
    && aquariusLoadout.Actions.Count == aquariusGraph.Abilities.Length,
    "Every Aquarius ability must unlock its registered action scaffold.");
var aquariusConfiguration = SimulationPlayerConfiguration.FromState(aquariusState);
Check(aquariusConfiguration.LearnedAbilities.SequenceEqual(aquariusGraph.Abilities.Select(node => node.Id.Value)
        .Order(StringComparer.Ordinal)),
    "Simulation configuration must report every resolved graph ability in stable order.");
foreach (var zodiac in Enum.GetValues<ZodiacSignId>())
{
    var signState = state with { Zodiac = zodiac };
    var graph = NexusRealms.Prelude.Database.Progression.ProgressionGraphs.All[zodiac];
    var loadout = signState.CreateGameState().Loadout;
    Check(loadout.LearnedAbilities.SetEquals(graph.Abilities.Select(node => node.Id)),
        $"{zodiac} must learn exactly its own graph's full ability set.");
    Check(loadout.Actions.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(graph.Abilities
            .Select(node => graph.AbilityDefinitionsById[node.Id].ActionId.Value)),
        $"{zodiac} must register every action unlocked by its graph.");
    Check(signState.CreateGameState().Zodiac == zodiac,
        $"{zodiac} must be applied to the runtime player.");
}
var cancerState = state with { Zodiac = ZodiacSignId.Cancer };
var cancerLoadout = cancerState.CreateGameState().Loadout;
Check(ReferenceEquals(
        NexusRealms.Prelude.Database.Progression.Cancer.Graph.AbilityDefinitionsById["Parry"],
        NexusRealms.Prelude.Database.Progression.Cancer.Parry)
    && cancerLoadout.Actions.ContainsKey("ability:Parry"),
    "Repeated IDs must resolve to the selected Zodiac's own ability definition.");
try
{
    _ = (state with { LearnedAbilities = ["Parry"] }).CreateGameState();
    throw new InvalidOperationException("An ability from another Zodiac must not resolve globally.");
}
catch (ArgumentException) { }
Check(SimulationRunner.Run(storyline, aquariusState, 42).PlayerActionUses.Keys
    .All(id => !aquariusGraph.Abilities.Where(node => !NexusRealms.Prelude.Actions.AbilityActionRegistry.Default.Resolve(aquariusGraph.AbilityDefinitionsById[node.Id]).IsImplemented).Select(node => aquariusGraph.AbilityDefinitionsById[node.Id].ActionId.Value).Contains(id)), "Unimplemented abilities must not become simulator actions.");
var startingLoadout = state.CreateGameState().Loadout;
Check(startingLoadout.Inventory.Single(item => item?.Id.Value == "starting-rocks")?.Quantity == 5,
    "Player must start with one inventory stack of five rocks.");
var first = SimulationRunner.Run(storyline, state, 42);
Check(first == SimulationRunner.Run(storyline, state, 42), "Seed replay or fresh-state isolation failed.");
DecisionTrace? capturedDecision = null;
_ = SimulationRunner.Run(storyline, state, 43, trace: decision => capturedDecision ??= decision);
Check(capturedDecision is { Selected: not null, Candidates.Count: > 0 },
    "A trace callback must capture the selected action and candidate evaluations.");
var traceText = SimulationReport.FormatDecisionTrace(capturedDecision!,
    new Dictionary<string, string> { ["player"] = "Player" },
    new Dictionary<string, string> { ["starting-sword"] = "Starting Sword" });
var omittedEquipmentLayouts = capturedDecision!.Candidates.Count(evaluation =>
    evaluation.Candidate.Kind == CandidateKind.ItemLayout
    && evaluation.Candidate.Action.Id == "equipment"
    && evaluation.Candidate.Key != capturedDecision.Selected!.Key);
Check(traceText.Contains("Chosen:", StringComparison.Ordinal)
    && traceText.Contains("Considered actions:", StringComparison.Ordinal)
    && traceText.Contains("utility=", StringComparison.Ordinal),
    "Decision trace output must explain the selected action and alternatives.");
Check(omittedEquipmentLayouts > 0
    && traceText.Contains($"Considered actions: {omittedEquipmentLayouts} equipment layouts omitted", StringComparison.Ordinal)
    && (!traceText.Contains("Equipment layout [", StringComparison.Ordinal)
        || capturedDecision.Selected is { Kind: CandidateKind.ItemLayout, Action.Id: "equipment" }),
    "Decision trace output must omit unselected equipment-layout alternatives.");
Check(state.Health == 20, "Simulation mutated its starting state.");
Check(first.PlayerStrategy == state.PlayerStrategy.Strategy, "Run must record the configured player strategy.");
Check(first.EnemyConfiguration.Length == ((CombatScenario)storyline.Nodes[NexusRealms.Prelude.Database.Chapter1.Part1.Battle1.Id]).Characters.Length,
    "Run must record the scenario's character configuration.");
Check(first.PlayerActionUses.Values.Sum() > 0, "Run must record player action usage.");
Check(first.PlayerActionResults.Length > 0 && first.PlayerActionResults.All(result =>
        !string.IsNullOrWhiteSpace(result.ActionId) && !string.IsNullOrWhiteSpace(result.TargetId) && result.Uses > 0),
    "Run must record aggregated player action results by stable target identifier.");
Check(first.PlayerFocus >= 0 && first.PlayerConsumables.All(item => item.Quantity >= 0),
    "Run must record ending player resources.");
Check(first.DecidingActionId is not null && first.DecidingActionSourceId is not null,
    "Run must record stable deciding-action identifiers.");
Check(!string.IsNullOrWhiteSpace(first.TerminalCondition), "Run must record its terminal condition.");
var outputJson = JsonSerializer.Serialize(new SimulationOutput(state.CurrentStoryNodeId, SimulationRunner.TurnLimit,
    new Dictionary<ZodiacSignId, SimulationPlayerConfiguration>
    {
        [state.Zodiac] = SimulationPlayerConfiguration.FromState(state),
    }, first.EnemyConfiguration, [first]));
using (var outputDocument = JsonDocument.Parse(outputJson))
{
    var root = outputDocument.RootElement;
    var playerConfiguration = root.GetProperty("PlayerConfigurations").GetProperty(nameof(ZodiacSignId.Aquarius));
    Check(root.GetProperty("ScenarioId").GetString() == state.CurrentStoryNodeId
        && root.GetProperty("TurnLimit").GetSingle() == SimulationRunner.TurnLimit
        && playerConfiguration.GetProperty("Items").GetArrayLength() > 0
        && playerConfiguration.GetProperty("ItemLayout").GetProperty("Right hand").GetString() == "starting-sword"
        && playerConfiguration.GetProperty("ActionIds").EnumerateArray()
            .Any(actionId => actionId.GetString() == "RightHandStrike")
        && root.GetProperty("EnemyConfiguration").GetArrayLength() == first.EnemyConfiguration.Length,
        "Simulation output must store shared scenario and player configuration at the top level.");
    var runJson = root.GetProperty("Runs")[0];
    Check(runJson.TryGetProperty("EndingTurn", out _) && !runJson.TryGetProperty("Turn", out _)
        && runJson.GetProperty("PlayerZodiac").GetInt32() == (int)ZodiacSignId.Aquarius
        && !runJson.TryGetProperty("EnemyConfiguration", out _)
        && runJson.TryGetProperty("PlayerActionResults", out _),
        "Run JSON must use EndingTurn, include action results, and omit shared enemy configuration.");
}
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
var authored = (CombatScenario)storyline.Nodes[NexusRealms.Prelude.Database.Chapter1.Part1.Battle1.Id];
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
Check(draw.Outcome == CombatOutcome.Draw && draw.EndingTurn == 100, "Full encounter did not draw at Turn 100.");
Check(state.PlayerStrategy.Strategy == StrategyId.Aggressive, "Simulation must default to a named deterministic policy.");
Check(state.PlayerZodiacs.SequenceEqual(Enum.GetValues<ZodiacSignId>()),
    "Default zodiac cycle must include every zodiac sign.");
Check(state.PlayerStrategies.Length == Enum.GetValues<StrategyId>().Length - 1
    && !state.PlayerStrategies.Contains(StrategyId.Protective), "Default strategy cycle must include each player-supported policy.");
var limitedState = state with
{
    PlayerZodiacs = [ZodiacSignId.Aries, ZodiacSignId.Taurus],
    PlayerStrategies = [StrategyId.Analytical, StrategyId.Cautious],
};
Check(limitedState.PlayerStrategyForRun(0).Strategy == StrategyId.Analytical
    && limitedState.PlayerStrategyForRun(1).Strategy == StrategyId.Cautious
    && limitedState.PlayerStrategyForRun(2).Strategy == StrategyId.Analytical,
    "Configured player strategies must cycle in order.");
Check(Enumerable.Range(0, 4).Select(limitedState.PlayerZodiacForRun)
        .SequenceEqual([ZodiacSignId.Aries, ZodiacSignId.Aries, ZodiacSignId.Taurus, ZodiacSignId.Taurus]),
    "Configured zodiac signs must each cycle across all configured strategies.");
var zodiacResult = SimulationRunner.Run(storyline, state with { Zodiac = ZodiacSignId.Pisces }, 42);
Check(zodiacResult.PlayerZodiac == ZodiacSignId.Pisces,
    "Simulation results must record the zodiac assigned to the player.");
Check(first != first with { PlayerZodiac = ZodiacSignId.Pisces },
    "Result equality must distinguish zodiac identities.");
var cycleLength = state.PlayerZodiacs.Length * state.PlayerStrategies.Length;
var combinations = Enumerable.Range(0, cycleLength)
    .Select(index => (state.PlayerZodiacForRun(index), state.PlayerStrategyForRun(index).Strategy)).ToArray();
Check(combinations.Distinct().Count() == cycleLength
    && state.PlayerZodiacForRun(cycleLength) == state.PlayerZodiacForRun(0),
    "A full cycle must cover every zodiac-strategy combination exactly once before wrapping.");
foreach (var invalidZodiacs in new ZodiacSignId[][] { [], [ZodiacSignId.Aries, ZodiacSignId.Aries], [(ZodiacSignId)(-1)] })
{
    try
    {
        _ = (state with { PlayerZodiacs = invalidZodiacs }).PlayerZodiacForRun(0);
        throw new Exception("Invalid zodiac cycle was accepted.");
    }
    catch (ArgumentException) { }
}
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
    && report.Contains("Player zodiac x strategy x enemy configuration x deciding action:", StringComparison.Ordinal),
    "Simulation report must include strategy rates, usage distributions, and strategy/action cross-tabs.");
var markdownReport = SimulationReport.FormatMarkdown(new SimulationOutput(state.CurrentStoryNodeId,
    SimulationRunner.TurnLimit, new Dictionary<ZodiacSignId, SimulationPlayerConfiguration>
    {
        [state.Zodiac] = SimulationPlayerConfiguration.FromState(state),
    }, first.EnemyConfiguration, reportSamples));
Check(markdownReport.Contains("## Outcomes", StringComparison.Ordinal)
    && markdownReport.Contains("## Strategy comparison", StringComparison.Ordinal)
    && markdownReport.Contains("## Zodiac and strategy comparison", StringComparison.Ordinal)
    && markdownReport.Contains("| Strategy | Runs | Victory (rate) |", StringComparison.Ordinal)
    && markdownReport.Contains("Avg. turn", StringComparison.Ordinal)
    && markdownReport.Contains("Throw [starting-rocks] × 4", StringComparison.Ordinal),
    "Markdown report must summarize outcomes and compare strategies with action-use totals.");
// Complete item-specific Throw choices are covered by AiDecisionChecks.
try
{
    SimulationRunner.Run(storyline, state with { CurrentStoryNodeId = "missing" }, 42);
    throw new Exception("Missing scenario was accepted.");
}
catch (ArgumentException) { }
// Validate the authored objective without prescribing an action sequence.
foreach (var replaySeed in new[] { 879543284, 879543289, 42 })
{
    var analyticalState = state with { Zodiac = ZodiacSignId.Aquarius, PlayerStrategy = new(StrategyId.Analytical) };
    var analyticalResult = SimulationRunner.Run(storyline, analyticalState, replaySeed);
    Check(analyticalResult.Outcome == CombatOutcome.Victory && analyticalResult.PlayerHealth > 0,
        "Analytical must discover a winning route with the full Aquarius repertoire, independent of the chosen actions.");
}
Console.WriteLine("Simulation checks passed.");
