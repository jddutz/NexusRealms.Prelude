using NexusRealms.Prelude;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.Database.Progression;
using NexusRealms.Prelude.DataModel;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var story = new Storyline();
Check(ProgressionGraphs.All.Count == 12, "All Zodiac graphs must exist.");
Check(story.Nodes.Values.OfType<ProgressionNode>().Count() == 20, "Twenty visits required.");
var layouts = new HashSet<string>();
foreach (var graph in ProgressionGraphs.All.Values)
{
    Check(graph.Branches.Length == 4 && graph.Branches.Select(b => b.Direction).Distinct().Count() == 4, "Four branches required.");
    layouts.Add(string.Join(";", graph.Branches.SelectMany(b => b.Abilities).Select(n => $"{n.X},{n.Y},{n.Parent}")));
    foreach (var branch in graph.Branches)
    {
        Check(branch.Abilities.Length == 5, "Five abilities per branch required.");
        var preceding = new HashSet<AbilityId>();
        foreach (var node in branch.Abilities)
        {
            Check(story.Abilities.ContainsKey(node.Ability), "Ability must be registered.");
            Check(node.Parent is null || preceding.Contains(node.Parent.Value), "Parent must precede its child in the same branch.");
            Check(preceding.Add(node.Ability), "Duplicate ability.");
        }
    }
    var state = new GameState(story.StartNodeId) { Zodiac = graph.Sign };
    var session = new ProgressionSession(state, story);
    for (var visit = 0; visit < 20; visit++)
    {
        var chapter = visit / 4 + 1;
        Check(session.Visit.Chapter == chapter, "Wrong chapter order.");
        Check(!session.Continue(), "Cannot skip learning.");
        foreach (var branch in graph.Branches)
            Check(session.IsUnlocked(branch.Direction) == ((int)branch.Direction < Math.Min(chapter, 4)), "Wrong branch unlock.");
        Check(!session.CanLearn("missing"), "Unknown ability cannot be learned.");
        var available = graph.Branches.SelectMany(b => b.Abilities).First(n => session.CanLearn(n.Ability));
        Check(session.Learn(available.Ability), "Available ability must be learnable.");
        Check(!session.Learn(available.Ability), "Visit cannot grant twice.");
        Check(!new ProgressionSession(state, story).CanLearn(available.Ability), "Reopening must preserve state.");
        Check(session.Continue(), "Learned visit must advance.");
    }
    Check(story.Nodes[state.CurrentStoryNodeId] is CombatScenario, "Final visit must lead to combat.");
    Check(state.Loadout.LearnedAbilities.Count == 20, "All twenty abilities learned.");
    Check(graph.Branches.SelectMany(b => b.Abilities).All(n => state.Loadout.LearnedAbilities.Contains(n.Ability)), "Graph must remain usable to completion.");
}
Check(layouts.Count == 12, "Zodiac layouts must differ.");
Console.WriteLine("All twelve Zodiac graphs, unlocks, prerequisites, visit limits, chapter routes, and final combat checks passed.");