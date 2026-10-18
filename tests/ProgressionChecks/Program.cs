using NexusRealms.Prelude.Rules;
using NexusRealms.Prelude;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.Database.Progression;
using NexusRealms.Prelude.DataModel;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var registry = NexusRealms.Prelude.Actions.AbilityActionRegistry.Default;
foreach (var definition in ProgressionGraphs.All.Values.SelectMany(graph => graph.AbilityDefinitions))
{
    var action = registry.Resolve(definition);
    Check(action.Id == definition.ActionId.Value && action.Icon == definition.Icon && action.Name == definition.Name,
        "Each ability must resolve one action with shared presentation.");
    if (!action.IsImplemented)
        Check(action.BuildEffects().IsEmpty && !action.IsImplemented, "Scaffolds must not inherit legacy attack damage.");
}
var authored = Aquarius.Heal with { TurnCost = 2.5f, FocusCost = 3, Targeting = new(TargetShape.Self) };
var bound = registry.Resolve(authored);
Check(bound.TurnCost == 2.5f && bound.Targeting == authored.Targeting
    && bound.Costs.Single().Amount == 3 && bound.Requirements.Abilities.Contains(authored.Id),
    "Binding must use ability costs, targeting and progression requirements.");
try { registry.Resolve(authored with { ActionId = "missing" }); throw new Exception("Unknown mapping accepted."); }
catch (ArgumentException) { }
var story = new Storyline();
Check(ProgressionGraphs.All.Count == 12, "All Zodiac graphs must exist.");
Check(story.StartNodeId == NexusRealms.Prelude.Database.Chapter1.Part1.Intro.Id,
    "The story must start at the authored Chapter1.Part1.Intro node.");
Check(story.Nodes.Values.OfType<ProgressNode>().Select(node => node.Id).ToHashSet()
    .SetEquals([NexusRealms.Prelude.Database.Chapter1.Part1.Progress1.Id]),
    "The post-victory progression visit must be registered.");
var progressionVisit = (ProgressNode)story.Nodes[NexusRealms.Prelude.Database.Chapter1.Part1.Progress1.Id];
var intro = story.Nodes[story.StartNodeId] as NarrativeNode
    ?? throw new Exception("The story start must be a narrative node.");
Check(intro.Choices.Length == 2
    && intro.Choices.Select(choice => choice.NextNode).Distinct().Count() == intro.Choices.Length
    && intro.Choices.All(choice => story.Nodes.ContainsKey(choice.NextNode)),
    "Each narrative choice must resolve to a distinct registered destination.");
Check(Cancer.Graph.AbilityDefinitionsById.Count == Cancer.Graph.Abilities.Length
    && ReferenceEquals(Cancer.Graph.AbilityDefinitionsById[Cancer.Parry.Id], Cancer.Parry)
    && ReferenceEquals(Aries.Graph.AbilityDefinitionsById[Aries.Brace.Id], Aries.Brace)
    && ReferenceEquals(Cancer.Graph.AbilityDefinitionsById[Cancer.Brace.Id], Cancer.Brace),
    "Ability IDs may repeat across Zodiacs while each graph owns its definitions.");
Check(Aries.Rampage.Targeting?.Shape == TargetShape.FrontRow
    && Aquarius.Whirlwind.Targeting?.Shape == TargetShape.FrontRow,
    "Rampage and Whirlwind must target the front row.");
Check(Aquarius.Definitions.Length == 20 && Aquarius.Graph.Abilities.Length == 20, "Aquarius must author all twenty definitions and placements.");
Check(Aquarius.Graph.Abilities.Select(node => (node.X, node.Y)).Distinct().Count() == 20,
    "Aquarius stars must have twenty explicit distinct artwork positions.");
Check(Aquarius.Graph.Abilities.All(node => node.X is >= -1 and <= 1 && node.Y is >= -1 and <= 1),
    "Aquarius stars must lie within the artwork coordinate space.");
var firstNode = Aquarius.Graph.Abilities[0];
var defaultRankNode = new AbilityNode(firstNode.Id, firstNode.Parent, firstNode.X, firstNode.Y);
var rankedNode = new AbilityNode(firstNode.Id, firstNode.Parent, firstNode.X, firstNode.Y, 18);
Check(defaultRankNode.Rank == 0 && rankedNode.Rank == 18,
    "Ability rank must default to zero and allow the maximum constructor rank.");
foreach (var definition in Aquarius.Definitions)
{
    Check(ReferenceEquals(story.GetAbilities(Aquarius.Graph.Sign)[definition.Id], definition), "Registry must use the authored Aquarius record.");
    Check(Aquarius.Graph.Abilities.Count(node => node.Id == definition.Id) == 1, "Each authored Aquarius ability must have one placement.");
}
var outsideVisit = new GameState(NexusRealms.Prelude.Database.Chapter1.Part1.Intro.Id);
Check(!ProgressionRules.Learn(outsideVisit, story, Aquarius.Heal.Id), "Learning requires a progression visit.");
var noPoints = new GameState(progressionVisit.Id) { Zodiac = ZodiacSignId.Aquarius, AbilityPoints = 0 };
Check(!ProgressionRules.Learn(noPoints, story, Aquarius.Heal.Id)
    && noPoints.Loadout.LearnedAbilities.Count == 0 && noPoints.CompletedProgressionNodes.Count == 0,
    "Insufficient points must reject learning without changing state.");
Check(ProgressionRules.CanContinue(noPoints, progressionVisit),
    "No points must not block continuing.");
var layouts = new HashSet<string>();
foreach (var graph in ProgressionGraphs.All.Values)
{
    var abilities = story.GetAbilities(graph.Sign);
    layouts.Add(string.Join(";", graph.Abilities.Select(n => $"{n.X},{n.Y},{n.Parent}")));
    Check(graph.AbilityDefinitionsById.Count == graph.Abilities.Length
        && graph.Abilities.All(node => graph.AbilityDefinitionsById.ContainsKey(node.Id)),
        "Each graph must own one definition for every placement.");
    Check(graph.Abilities.Select(node => node.Id).Distinct().Count() == graph.Abilities.Length,
        "Each ability must have one placement per graph.");
    var fresh = new GameState(progressionVisit.Id) { Zodiac = graph.Sign };
    foreach (var node in graph.Abilities)
    {
        Check(abilities.ContainsKey(node.Id), "Ability must be registered in its own Zodiac graph.");
        Check(ProgressionRules.CanLearn(fresh, story, node.Id) == (node.Parent is null),
            "Only abilities without prerequisites must be learnable initially.");
        var ancestors = new HashSet<AbilityId> { node.Id };
        var parent = node.Parent;
        while (parent is { } parentId)
        {
            Check(ancestors.Add(parentId), "Prerequisite connections must not contain cycles.");
            var parentNode = graph.Abilities.SingleOrDefault(candidate => candidate.Id == parentId);
            Check(parentNode is not null, "Prerequisite must belong to the same Zodiac graph.");
            parent = parentNode!.Parent;
        }
    }
    var state = new GameState(progressionVisit.Id) { Zodiac = graph.Sign };
    Check(state.AbilityPoints == 20, "Players start with twenty ability points.");
    Check(ProgressionRules.CanContinue(state, progressionVisit),
        "Players may continue without spending available ability points.");
    Check(!ProgressionRules.CanLearn(state, story, "missing"), "Unknown ability cannot be learned.");
    var firstAbility = graph.Abilities.First(n => ProgressionRules.CanLearn(state, story, n.Id));
    Check(ProgressionRules.Learn(state, story, firstAbility.Id), "Available ability must be learnable.");
    Check(state.AbilityPoints == 19, "Learning must spend exactly one point.");
    Check(!ProgressionRules.Learn(state, story, firstAbility.Id), "An ability cannot be learned twice.");
    Check(state.AbilityPoints == 19, "Rejected learning must not spend points.");
    Check(ProgressionRules.CanContinue(state, progressionVisit), "Learning must enable Continue.");
    state.CurrentStoryNodeId = progressionVisit.NextNode;
    Check(state.CurrentStoryNodeId == NexusRealms.Prelude.Database.Chapter1.Part1.Battle1.Id,
        "Continue must advance to the next authored encounter.");
    Check(!ProgressionRules.CanContinue(state, progressionVisit), "The old visit cannot advance the story twice.");
    Check(!ProgressionRules.CanLearn(state, story, firstAbility.Id), "Learning is unavailable in combat.");
    var singleVisit = new GameState(progressionVisit.Id) { Zodiac = graph.Sign };
    for (var learnedCount = 0; learnedCount < graph.Abilities.Length; learnedCount++)
    {
        foreach (var node in graph.Abilities)
        {
            var expected = !singleVisit.Loadout.LearnedAbilities.Contains(node.Id)
                && singleVisit.Loadout.LearnedAbilities.Count >= node.Rank
                && (node.Parent is null || singleVisit.Loadout.LearnedAbilities.Contains(node.Parent.Value));
            Check(ProgressionRules.CanLearn(singleVisit, story, node.Id) == expected,
                "Availability must refresh after each purchase in the same visit.");
        }
        var available = graph.Abilities.First(node => ProgressionRules.CanLearn(singleVisit, story, node.Id));
        Check(ProgressionRules.Learn(singleVisit, story, available.Id), "Multiple purchases per visit must succeed.");
        Check(singleVisit.AbilityPoints == 19 - learnedCount, "Each purchase must spend exactly one point.");
    }
    Check(graph.Abilities.All(n => singleVisit.Loadout.LearnedAbilities.Contains(n.Id)),
        "Every graph must remain usable to completion in one visit.");
    singleVisit.CompletedProgressionNodes.Clear();
    Check(ProgressionRules.CanContinue(singleVisit, progressionVisit),
        "A visit must allow continuing after all points are spent.");
    singleVisit.AbilityPoints = 1;
    Check(ProgressionRules.CanContinue(singleVisit, progressionVisit),
        "A fully learned graph must allow continuing even with points remaining.");
}
Check(layouts.Count == 12, "Zodiac layouts must differ.");
Console.WriteLine("All twelve Zodiac graphs, graph-scoped definitions, unlocks, prerequisites, and multiple purchases passed.");