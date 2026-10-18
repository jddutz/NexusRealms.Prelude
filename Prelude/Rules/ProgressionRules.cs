namespace NexusRealms.Prelude.Rules;

/// <summary>Reusable learning rules over the current game state and authored definitions.</summary>
public static class ProgressionRules
{
    /// <summary>Reports whether an ability has been learned during this visit.</summary>
    public static bool HasLearnedThisVisit(GameState state, ProgressNode visit) =>
        state.CompletedProgressionNodes.Contains(visit.Id);

    /// <summary>Allows continuing from the active progression visit, whether or not points were spent.</summary>
    public static bool CanContinue(GameState state, ProgressNode visit) =>
        state.CurrentStoryNodeId == visit.Id;

    /// <summary>Checks remaining points, the current visit, the ability's rank, and its prerequisite.</summary>
    public static bool CanLearn(GameState state, Storyline story, AbilityId id)
    {
        var abilities = story.GetAbilities(state.Zodiac);
        if (state.AbilityPoints <= 0 || !story.Nodes.TryGetValue(state.CurrentStoryNodeId, out var current)
            || current is not ProgressNode
            || state.Loadout.LearnedAbilities.Contains(id) || !abilities.ContainsKey(id)) return false;
        var graph = Database.Progression.ProgressionGraphs.All[state.Zodiac];
        return graph.Abilities.Any(node => node.Id == id
            && state.Loadout.LearnedAbilities.Count >= node.Rank
            && (node.Parent is null || state.Loadout.LearnedAbilities.Contains(node.Parent.Value)));
    }

    /// <summary>Learns an eligible ability and spends one point, allowing further purchases this visit.</summary>
    public static bool Learn(GameState state, Storyline story, AbilityId id)
    {
        if (!CanLearn(state, story, id)) return false;
        state.Loadout.Learn(story.GetAbilities(state.Zodiac)[id]);
        state.AbilityPoints--;
        state.CompletedProgressionNodes.Add(state.CurrentStoryNodeId);
        return true;
    }
}
