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
    public FrozenDictionary<StatusEffectId, StatusEffectData> StatusEffects { get; } =
        Database.StatusEffects.All;

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
        foreach (var status in StatusEffects.Values)
            CombatRules.ValidateStatus(status);
        foreach (var character in Characters.Values)
        {
            _ = Combat.CombatLoadout.FromCharacter(character);
            foreach (var action in character.Actions)
                CombatRules.Validate(action, StatusEffects);
            foreach (var id in character.StatusEffects)
                if (!StatusEffects.ContainsKey(id))
                    throw new InvalidOperationException(
                        $"Character '{character.Id}' references unknown status '{id}'."
                    );
        }
        foreach (var graph in Progression.ProgressionGraphs.All.Values)
        {
            var abilities = graph.AbilityDefinitionsById;
            var abilityActions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ability in graph.AbilityDefinitions)
            {
                if (string.IsNullOrWhiteSpace(ability.Id.Value))
                    throw new ArgumentException($"Progression '{graph.Sign}' contains an ability with no identity.");
                var action = AbilityActionRegistry.Default.Resolve(ability);
                if (!abilityActions.Add(action.Id))
                    throw new ArgumentException($"Progression '{graph.Sign}' contains duplicate ability action '{action.Id}'.");
                if (action.Requirements.Abilities.Any(id => !abilities.ContainsKey(id)))
                    throw new ArgumentException($"Action '{action.Id}' references an ability outside progression '{graph.Sign}'.");
                CombatRules.Validate(action, StatusEffects);
            }

            var graphNodes = new HashSet<AbilityId>();
            foreach (var node in graph.Abilities)
                if (
                    !graphNodes.Add(node.Id)
                    || !abilities.ContainsKey(node.Id)
                    || !float.IsFinite(node.X)
                    || !float.IsFinite(node.Y)
                )
                    throw new ArgumentException($"Invalid progression node '{node.Id}'.");
            foreach (var node in graph.Abilities)
            {
                var ancestors = new HashSet<AbilityId> { node.Id };
                var parent = node.Parent;
                while (parent is { } id)
                {
                    var predecessor = graph.Abilities.FirstOrDefault(candidate =>
                        candidate.Id == id
                    );
                    if (predecessor is null || !ancestors.Add(id))
                        throw new ArgumentException(
                            $"Invalid progression prerequisite for '{node.Id}'."
                        );
                    parent = predecessor.Parent;
                }
            }
        }
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
                CombatRules.Validate(occurrence.Action, StatusEffects);
            }
        }
        }

        /// <summary>Gets the ability definitions owned by a Zodiac progression.</summary>
        /// <param name="zodiac">The Zodiac whose ability definitions are requested.</param>
        /// <returns>The graph-local ability definitions keyed by ability ID.</returns>
        public FrozenDictionary<AbilityId, AbilityData> GetAbilities(ZodiacSignId zodiac) =>
            Progression.ProgressionGraphs.All[zodiac].AbilityDefinitionsById;
    }
