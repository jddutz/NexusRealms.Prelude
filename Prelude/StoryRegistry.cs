namespace NexusRealms.Prelude;

/// <summary>
/// Provides runtime lookups and validates references in an authored story graph.
/// </summary>
public sealed class StoryRegistry
{
    /// <summary>
    /// Gets the characters declared by the story graphs, keyed by character identifier.
    /// </summary>
    public FrozenDictionary<CharacterId, CharacterData> Characters { get; }

    /// <summary>
    /// Gets the nodes declared by the story graphs, keyed by node identifier.
    /// </summary>
    public FrozenDictionary<StoryNodeId, StoryNode> Nodes { get; }

    /// <summary>
    /// Gets the identifier of the first node in the story.
    /// </summary>
    public StoryNodeId Start { get; }

    /// <summary>
    /// Gets the identifier of the final node in the story.
    /// </summary>
    public StoryNodeId End { get; }

    /// <summary>
    /// Indexes the characters and nodes exposed by an authored story graph.
    /// </summary>
    public StoryRegistry(IStoryGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        Start = graph.Start;
        End = graph.End;
        var characters = new Dictionary<CharacterId, CharacterData>();
        var nodes = new Dictionary<StoryNodeId, StoryNode>();
        var type = graph.GetType();
        foreach (var character in graph.Characters)
        {
            if (character.Id == CharacterId.Invalid)
            {
                throw new InvalidOperationException(
                    $"Story graph '{type.FullName}' declares a character with an invalid ID."
                );
            }

            if (!characters.TryAdd(character.Id, character))
            {
                throw new InvalidOperationException(
                    $"Duplicate character ID '{character.Id}' in story graph '{type.FullName}'."
                );
            }
        }

        foreach (var node in graph.Nodes)
        {
            if (node.Id == StoryNodeId.Invalid)
            {
                throw new InvalidOperationException(
                    $"Story graph '{type.FullName}' declares a node with an invalid ID."
                );
            }

            if (!nodes.TryAdd(node.Id, node))
            {
                throw new InvalidOperationException(
                    $"Duplicate story node ID '{node.Id}' in story graph '{type.FullName}'."
                );
            }
        }

        // Include the containing segments without re-enumerating their aggregated definitions.
        var graphs = new HashSet<IStoryGraph>(ReferenceEqualityComparer.Instance) { graph };
        foreach (var node in nodes.Values)
        {
            var ancestors = new HashSet<IStoryGraph>(ReferenceEqualityComparer.Instance);
            for (IStoryGraph? segment = node.Subgraph; segment is not null; segment = segment.Parent)
            {
                if (!ancestors.Add(segment))
                    throw new InvalidOperationException($"Story node '{node.Id}' has a cyclic graph hierarchy.");
                graphs.Add(segment);
            }
        }
        foreach (var segment in graphs)
        {
            if (!nodes.ContainsKey(segment.Start))
                throw new InvalidOperationException($"Story graph '{segment.GetType().FullName}' references missing start node '{segment.Start}'.");
            if (segment.End != StoryNodeId.Invalid && !nodes.ContainsKey(segment.End))
                throw new InvalidOperationException($"Story graph '{segment.GetType().FullName}' references missing end node '{segment.End}'.");
        }

        foreach (var node in nodes.Values)
        {
            if (node is NarrativeNode narrative)
            {
                if (
                    string.IsNullOrWhiteSpace(narrative.BackgroundImage.Value)
                    || string.IsNullOrWhiteSpace(narrative.ForegroundImage.Value)
                    || string.IsNullOrWhiteSpace(narrative.RichText)
                    || narrative.Choices.IsDefaultOrEmpty
                )
                    throw new InvalidOperationException($"Invalid narrative node '{node.Id}'.");

                foreach (var choice in narrative.Choices)
                {
                    if (
                        string.IsNullOrWhiteSpace(choice.Text)
                        || !nodes.ContainsKey(choice.NextNode)
                    )
                        throw new InvalidOperationException(
                            $"Narrative node '{node.Id}' has an invalid choice destination '{choice.NextNode}'."
                        );
                }
            }

            if (node is ProgressNode progression)
            {
                if (progression.Chapter is < 1 or > 5 || !nodes.ContainsKey(progression.NextNode))
                    throw new InvalidOperationException($"Invalid progression visit '{node.Id}'.");
            }
            if (node is OutcomeNode { NextNode: { } nextNode } && !nodes.ContainsKey(nextNode))
                throw new InvalidOperationException(
                    $"Outcome node '{node.Id}' references missing destination '{nextNode}'."
                );

            if (node is not CombatScenario scenario)
            {
                continue;
            }

            foreach (
                var destination in new[]
                {
                    scenario.VictoryNode,
                    scenario.DefeatNode,
                    scenario.EscapeNode,
                }
            )
                if (destination is { } id && !nodes.ContainsKey(id))
                    throw new InvalidOperationException(
                        $"Combat scenario '{node.Id}' references missing outcome node '{id}'."
                    );
            var assignedSlots = new HashSet<FormationSlot>();
            foreach (var placement in scenario.Characters)
            {
                if (!characters.ContainsKey(placement.CharacterId))
                {
                    throw new InvalidOperationException(
                        $"Combat scenario '{node.Id}' references missing character '{placement.CharacterId}'."
                    );
                }

                if (!Enum.IsDefined(placement.Slot))
                {
                    throw new InvalidOperationException(
                        $"Combat scenario '{node.Id}' uses invalid formation slot '{placement.Slot}'."
                    );
                }

                if (!assignedSlots.Add(placement.Slot))
                {
                    throw new InvalidOperationException(
                        $"Combat scenario '{node.Id}' assigns multiple characters to formation slot '{placement.Slot}'."
                    );
                }
            }
        }

        Characters = characters.ToFrozenDictionary();
        Nodes = nodes.ToFrozenDictionary();
        RuntimeDefinitions.Validate(Characters.Values);
        foreach (var scenario in Nodes.Values.OfType<CombatScenario>())
        {
            _ = scenario.BuildVictoryConditions();
            _ = scenario.BuildDefeatConditions();
            foreach (var occurrence in scenario.Events)
            {
                if (!float.IsFinite(occurrence.Turn) || occurrence.Turn < 0)
                    throw new ArgumentException("Invalid authored event Turn.");
                var recipients = Enumerable
                    .Range(0, scenario.Characters.Length)
                    .Select(index => $"encounter-{index}")
                    .Append("player")
                    .ToHashSet();
                if (
                    !recipients.Contains(occurrence.SourceId)
                    || !recipients.Contains(occurrence.RecipientId)
                )
                    throw new ArgumentException(
                        "Authored event references an unresolved character."
                    );
                CombatRules.Validate(occurrence.Action, Database.StatusEffects.All);
            }
        }
    }
}
