using System.Text.Json;
using System.Text.Json.Serialization;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;
using NexusRealms.Prelude.Simulator;

try
{
    if (args.Contains("--help"))
    {
        Console.WriteLine("Usage: Simulator --state <file.json> [--runs <positive integer>] [--seed <integer>] [--output <results.json>]");
        Console.WriteLine("Omit --runs to run once per configured player strategy.");
        return 0;
    }
    var options = new Dictionary<string, string>();
    for (var i = 0; i < args.Length; i += 2)
    {
        if (args[i] is not ("--state" or "--runs" or "--seed" or "--output") || i + 1 >= args.Length || !options.TryAdd(args[i], args[i + 1]))
            throw new ArgumentException($"Invalid or duplicate option '{args[i]}'. Use --help for usage.");
    }
    if (!options.TryGetValue("--state", out var path)) throw new ArgumentException("--state is required. Use --help for usage.");
    var requestedRuns = options.TryGetValue("--runs", out var count) ? int.Parse(count) : (int?)null;
    if (requestedRuns is { } configuredRuns) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(configuredRuns);
    var seed = options.TryGetValue("--seed", out var seedText) ? int.Parse(seedText) : Random.Shared.Next();
    var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    json.Converters.Add(new JsonStringEnumConverter());
    var state = JsonSerializer.Deserialize<SimulationState>(File.ReadAllText(path), json) ?? throw new ArgumentException("State must be a JSON object.");
    _ = state.PlayerStrategyForRun(0);
    var strategyCycle = state.PlayerStrategies
        .Select(strategy => state.PlayerStrategy with { Strategy = strategy }).ToArray();
    var runs = requestedRuns ?? strategyCycle.Length;
    var storyline = new Storyline();
    if (!storyline.Nodes.TryGetValue(state.CurrentStoryNodeId, out var node) || node is not CombatScenario scenario)
        throw new ArgumentException($"'{state.CurrentStoryNodeId}' is not an authored combat scenario.");
    var characterNames = new Dictionary<string, string>(StringComparer.Ordinal) { ["player"] = "Player" };
    var itemNames = state.CreateGameState().Loadout.Items.ToDictionary(item => item.Id.Value, item => item.Name, StringComparer.Ordinal);
    for (var index = 0; index < scenario.Characters.Length; index++)
    {
        var definition = storyline.Characters[scenario.Characters[index].CharacterId];
        characterNames[$"encounter-{index}"] = definition.Name;
        foreach (var item in definition.Items) itemNames[item.Id.Value] = item.Name;
    }
    var results = new List<SimulationResult>();
    Console.WriteLine($"Scenario: {state.CurrentStoryNodeId}; runs: {runs}; seed: {seed}; turn limit: {SimulationRunner.TurnLimit}");
    Console.WriteLine($"Player strategy cycle: {string.Join(", ", strategyCycle.Select(assignment => assignment.Strategy))}");
    for (var run = 0; run < runs; run++)
    {
        var runSeed = unchecked(seed + run);
        var strategy = strategyCycle[run % strategyCycle.Length];
        Console.WriteLine($"Starting simulation {run + 1}/{runs} ({strategy.Strategy}; seed {runSeed}).");
        results.Add(SimulationRunner.Run(storyline, state with { PlayerStrategy = strategy }, runSeed,
            decision => Console.Write(SimulationReport.FormatDecisionTrace(decision, characterNames, itemNames))));
    }
    if (options.TryGetValue("--output", out var output)) File.WriteAllText(output, JsonSerializer.Serialize(results, json));
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or IOException or JsonException or FormatException or OverflowException or InvalidOperationException)
{
    Console.Error.WriteLine($"Simulation failed: {exception.Message}");
    return 1;
}
