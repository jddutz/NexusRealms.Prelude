namespace NexusRealms.Prelude.Database;

/// <summary>
/// Provides the immutable Prelude story database and starting node.
/// </summary>
public sealed class Storyline
{
    /// <summary>
    /// Gets the characters declared by the story graphs, keyed by character identifier.
    /// </summary>
    public FrozenDictionary<CharacterId, CharacterData> Characters { get; }

    /// <summary>Gets the authored status effect definitions keyed by identifier.</summary>
    public FrozenDictionary<StatusEffectId, StatusEffectData> StatusEffects { get; } = Database.StatusEffects.All;

    /// <summary>Gets the authored ability definitions keyed by identifier.</summary>
    public FrozenDictionary<AbilityId, AbilityData> Abilities { get; } = Database.Abilities.All;

    /// <summary>
    /// Gets the nodes declared by the story graphs, keyed by node identifier.
    /// </summary>
    public FrozenDictionary<StoryNodeId, StoryNode> Nodes { get; }

    /// <summary>
    /// Gets the identifier of the first node in the story.
    /// </summary>
    public StoryNodeId StartNodeId { get; } = Chapter1.Part1.Intro.Id;

    /// <summary>
    /// Gets the identifier of the final node in the story.
    /// </summary>
    public StoryNodeId EndNodeId { get; } = Chapter1.Part1.Intro.Id;

    /// <summary>
    /// Discovers chapter graphs in the database assembly and registers their characters and nodes.
    /// </summary>
    public Storyline()
    {
        var characters = new Dictionary<CharacterId, CharacterData>();
        var nodes = new Dictionary<StoryNodeId, StoryNode>();
        var graphs = new List<(IStoryGraph Graph, string TypeName)>();
        var chapterNamespacePrefix = $"{typeof(Storyline).Namespace}.Chapter";
        var storyGraphTypes = typeof(Storyline)
            .Assembly.GetTypes()
            .Where(type =>
                type.Namespace?.StartsWith(chapterNamespacePrefix, StringComparison.Ordinal) == true
                && typeof(IStoryGraph).IsAssignableFrom(type)
                && !type.IsInterface
                && !type.IsAbstract
                && !type.ContainsGenericParameters
            )
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var type in storyGraphTypes)
        {
            var graph = (IStoryGraph)(
                Activator.CreateInstance(type)
                ?? throw new InvalidOperationException(
                    $"Could not instantiate story graph type '{type.FullName}'."
                )
            );
            graphs.Add((graph, type.FullName ?? type.Name));

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
        }

        foreach (var (graph, typeName) in graphs)
        {
            if (!nodes.ContainsKey(graph.Start))
            {
                throw new InvalidOperationException(
                    $"Story graph '{typeName}' references missing start node '{graph.Start}'."
                );
            }

            if (graph.End != StoryNodeId.Invalid && !nodes.ContainsKey(graph.End))
            {
                throw new InvalidOperationException(
                    $"Story graph '{typeName}' references missing end node '{graph.End}'."
                );
            }
        }

        foreach (var node in nodes.Values)
        {
            if (node is not CombatScenario scenario)
            {
                continue;
            }

            foreach (var destination in new[] { scenario.VictoryNode, scenario.DefeatNode, scenario.EscapeNode })
                if (destination is { } id && !nodes.ContainsKey(id))
                    throw new InvalidOperationException($"Combat scenario '{node.Id}' references missing outcome node '{id}'.");
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

        if (!nodes.ContainsKey(StartNodeId))
        {
            throw new InvalidOperationException(
                $"The storyline start node '{StartNodeId}' was not registered."
            );
        }

        if (!nodes.ContainsKey(EndNodeId))
        {
            throw new InvalidOperationException(
                $"The storyline end node '{EndNodeId}' was not registered."
            );
        }

        Characters = characters.ToFrozenDictionary();
        Nodes = nodes.ToFrozenDictionary();
    }
}
