namespace NexusRealms.Prelude;

/// <summary>Loads the scene associated with the game's current story node.</summary>
internal static class StorySceneRouter
{
    /// <summary>Loads the scene corresponding to the active story node.</summary>
    /// <param name="state">The mutable game state containing the current node identifier.</param>
    /// <param name="storyline">The registered story graph.</param>
    /// <param name="scenes">The scene manager used to load the matching scene.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="InvalidOperationException">The node is missing or has no matching scene.</exception>
    internal static void LoadCurrentNode(GameState state, StoryRegistry storyline, ISceneManager scenes)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(storyline);
        ArgumentNullException.ThrowIfNull(scenes);
        if (!storyline.Nodes.TryGetValue(state.CurrentStoryNodeId, out var node))
            throw new InvalidOperationException(
                $"Current story node '{state.CurrentStoryNodeId}' is not registered."
            );

        switch (node)
        {
            case CombatScenario:
                scenes.LoadScene<CombatScene>();
                break;
            case NarrativeNode:
                scenes.LoadScene<NarrativeScene>();
                break;
            case OutcomeNode:
                scenes.LoadScene<OutcomeScene>();
                break;
            case ProgressNode:
                scenes.LoadScene<ProgressionScene>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Story node '{node.Id}' has no registered scene."
                );
        }
    }
}
