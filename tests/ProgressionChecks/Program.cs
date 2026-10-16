using NexusRealms.Prelude.Rules;
using NexusRealms.Prelude;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.Database.Progression;
using NexusRealms.Prelude.DataModel;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var story = new Storyline();
Check(ProgressionGraphs.All.Count == 12, "All Zodiac graphs must exist.");
Check(story.Nodes.Values.OfType<ProgressNode>().Count(node => node.Id.Value?.Contains("_progression_", StringComparison.Ordinal) == true) == 20, "Twenty chapter progression visits required.");
Check(Aquarius.Definitions.Length == 20 && Aquarius.Graph.Abilities.Length == 20, "Aquarius must author all twenty definitions and placements.");
foreach (var definition in Aquarius.Definitions)
{
    Check(ReferenceEquals(story.Abilities[definition.Id], definition), "Registry must use the authored Aquarius record.");
    Check(Aquarius.Graph.Abilities.Count(node => node.Id == definition.Id) == 1, "Each authored Aquarius ability must have one placement.");
}
foreach (var direction in Enum.GetValues<DirectionEnum>())
{
    var fresh = new GameState(story.StartNodeId) { Zodiac = ZodiacSign.Aquarius };
    var root = Aquarius.Graph.Abilities.First(node => node.Direction == direction && node.Parent is null);
    Check(ProgressionRules.CanLearn(fresh, story, root.Id), "Display direction must not gate learning by chapter.");
}
var outsideVisit = new GameState(NexusRealms.Prelude.Database.Chapter1.Part1.Intro.Id);
Check(!ProgressionRules.Learn(outsideVisit, story, Aquarius.Heal.Id), "Learning requires a progression visit.");
var noPoints = new GameState(story.StartNodeId) { Zodiac = ZodiacSign.Aquarius, AbilityPoints = 0 };
Check(!ProgressionRules.Learn(noPoints, story, Aquarius.Heal.Id)
    && noPoints.Loadout.LearnedAbilities.Count == 0 && noPoints.CompletedProgressionNodes.Count == 0,
    "Insufficient points must reject learning without changing state.");
var layouts = new HashSet<string>();
foreach (var graph in ProgressionGraphs.All.Values)
{
    Check(graph.Abilities.Select(node => node.Direction).Distinct().Count() == 4, "Four directions required.");
    layouts.Add(string.Join(";", graph.Abilities.Select(n => $"{n.X},{n.Y},{n.Parent}")));
    foreach (var direction in Enum.GetValues<DirectionEnum>())
    {
        Check(graph.Abilities.Count(node => node.Direction == direction) > 0, "Each branch must contain abilities.");
        var preceding = new HashSet<AbilityId>();
        foreach (var node in graph.Abilities.Where(node => node.Direction == direction))
        {
            Check(story.Abilities.ContainsKey(node.Id), "Ability must be registered.");
            Check(node.Parent is null || preceding.Contains(node.Parent.Value), "Parent must precede its child in the same branch.");
            Check(preceding.Add(node.Id), "Duplicate ability.");
        }
    }
    var state = new GameState(story.StartNodeId) { Zodiac = graph.Sign };
    Check(state.AbilityPoints == 20, "Players start with twenty ability points.");
    for (var visit = 0; visit < 20; visit++)
    {
        var chapter = visit / 4 + 1;
        Check(((ProgressNode)story.Nodes[state.CurrentStoryNodeId]).Chapter == chapter, "Wrong chapter order.");
        var currentVisit = (ProgressNode)story.Nodes[state.CurrentStoryNodeId];
        Check(!ProgressionRules.CanContinue(state, currentVisit), "Cannot skip learning.");
        Check(!ProgressionRules.CanLearn(state, story, "missing"), "Unknown ability cannot be learned.");
        var available = graph.Abilities.First(n => ProgressionRules.CanLearn(state, story, n.Id));
        Check(ProgressionRules.Learn(state, story, available.Id), "Available ability must be learnable.");
        Check(state.AbilityPoints == 19 - visit, "Learning must spend exactly one point.");
        Check(!ProgressionRules.Learn(state, story, available.Id), "Visit cannot grant twice.");
        Check(state.AbilityPoints == 19 - visit, "Rejected learning must not spend points.");
        Check(!ProgressionRules.CanLearn(state, story, available.Id), "Reopening must preserve state.");
        Check(ProgressionRules.CanContinue(state, currentVisit), "Learned visit must allow continuing.");
        state.CurrentStoryNodeId = currentVisit.NextNode;
    }
    Check(story.Nodes[state.CurrentStoryNodeId] is CombatScenario, "Final visit must lead to combat.");
    Check(state.Loadout.LearnedAbilities.Count == 20, "All twenty abilities learned.");
    Check(graph.Abilities.All(n => state.Loadout.LearnedAbilities.Contains(n.Id)), "Graph must remain usable to completion.");
}
Check(layouts.Count == 12, "Zodiac layouts must differ.");
Console.WriteLine("All twelve Zodiac graphs, unlocks, prerequisites, visit limits, chapter routes, and final combat checks passed.");