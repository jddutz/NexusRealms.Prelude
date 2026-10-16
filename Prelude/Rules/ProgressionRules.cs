namespace NexusRealms.Prelude.Rules;

/// <summary>Reusable learning rules over the current game state and authored definitions.</summary>
public static class ProgressionRules
{
    public static bool HasLearnedThisVisit(GameState state, ProgressNode visit) =>
        state.CompletedProgressionNodes.Contains(visit.Id);

    public static bool CanContinue(GameState state, ProgressNode visit) =>
        state.CurrentStoryNodeId == visit.Id && HasLearnedThisVisit(state, visit);

    public static bool CanLearn(GameState state, Storyline story, AbilityId id)
    {
        if (state.AbilityPoints <= 0 || !story.Nodes.TryGetValue(state.CurrentStoryNodeId, out var current)
            || current is not ProgressNode visit || HasLearnedThisVisit(state, visit)
            || state.Loadout.LearnedAbilities.Contains(id) || !story.Abilities.ContainsKey(id)) return false;
        var graph = Database.Progression.ProgressionGraphs.All[state.Zodiac];
        return graph.Abilities.Any(node => node.Id == id
            && (node.Parent is null || state.Loadout.LearnedAbilities.Contains(node.Parent.Value)));
    }

    public static bool Learn(GameState state, Storyline story, AbilityId id)
    {
        if (!CanLearn(state, story, id)) return false;
        state.Loadout.Learn(story.Abilities[id]);
        state.AbilityPoints--;
        state.CompletedProgressionNodes.Add(state.CurrentStoryNodeId);
        return true;
    }
}
