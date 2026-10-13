namespace NexusRealms.Prelude;

/// <summary>Runtime learning rules; authored graphs contain no mutable player state.</summary>
public sealed class ProgressionSession(GameState state, Storyline story)
{
    public ProgressionGraph Graph => Database.Progression.ProgressionGraphs.All[state.Zodiac];
    public ProgressionNode Visit => story.Nodes[state.CurrentStoryNodeId] as ProgressionNode
        ?? throw new InvalidOperationException("Expected a progression story node.");
    public bool HasLearnedThisVisit => state.CompletedProgressionNodes.Contains(Visit.Id);
    public bool IsUnlocked(ProgressionBranch branch) => (int)branch < Math.Min(Visit.Chapter, 4);
    public bool CanLearn(AbilityId id) => !HasLearnedThisVisit && Graph.Branches.Any(branch =>
        IsUnlocked(branch.Direction) && branch.Abilities.Any(node => node.Ability == id
        && !state.Loadout.LearnedAbilities.Contains(id)
        && (node.Parent is null || state.Loadout.LearnedAbilities.Contains(node.Parent.Value))));
    public bool Learn(AbilityId id)
    {
        if (!CanLearn(id)) return false;
        state.Loadout.Learn(story.Abilities[id]);
        state.CompletedProgressionNodes.Add(Visit.Id);
        return true;
    }
    public bool Continue()
    {
        if (!HasLearnedThisVisit) return false;
        state.CurrentStoryNodeId = Visit.NextNode;
        return true;
    }
}