using System.Text.Json;
using System.Text.Json.Serialization;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.Simulator;

try
{
    if (args.Contains("--help"))
    {
        Console.WriteLine("Usage: Simulator --state <file.json> [--runs <positive integer>] [--seed <integer>] [--output <results.json>]");
        return 0;
    }
    var options = new Dictionary<string, string>();
    for (var i = 0; i < args.Length; i += 2)
    {
        if (args[i] is not ("--state" or "--runs" or "--seed" or "--output") || i + 1 >= args.Length || !options.TryAdd(args[i], args[i + 1]))
            throw new ArgumentException($"Invalid or duplicate option '{args[i]}'. Use --help for usage.");
    }
    if (!options.TryGetValue("--state", out var path)) throw new ArgumentException("--state is required. Use --help for usage.");
    var runs = options.TryGetValue("--runs", out var count) ? int.Parse(count) : 1;
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runs);
    var seed = options.TryGetValue("--seed", out var seedText) ? int.Parse(seedText) : Random.Shared.Next();
    var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    json.Converters.Add(new JsonStringEnumConverter());
    var state = JsonSerializer.Deserialize<SimulationState>(File.ReadAllText(path), json) ?? throw new ArgumentException("State must be a JSON object.");
    var storyline = new Storyline();
    var results = new List<SimulationResult>();
    for (var run = 0; run < runs; run++) results.Add(SimulationRunner.Run(storyline, state, unchecked(seed + run)));
    Console.WriteLine($"Scenario: {state.CurrentStoryNodeId}; runs: {runs}; seed: {seed}; turn limit: {SimulationRunner.TurnLimit}");
    foreach (var group in results.GroupBy(result => result.Outcome).OrderBy(group => group.Key))
        Console.WriteLine($"{group.Key}: {group.Count()} ({100d * group.Count() / runs:F2}%)");
    Console.WriteLine($"Mean ending turn: {results.Average(result => result.Turn):F2}; mean remaining health: {results.Average(result => result.PlayerHealth):F2}");
    Console.WriteLine(SimulationReport.Format(results));
    if (options.TryGetValue("--output", out var output)) File.WriteAllText(output, JsonSerializer.Serialize(results, json));
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or IOException or JsonException or FormatException or OverflowException or InvalidOperationException)
{
    Console.Error.WriteLine($"Simulation failed: {exception.Message}");
    return 1;
}
