namespace NexusRealms.Prelude;

/// <summary>Validates combat and progression definitions independently of the story graph.</summary>
internal static class RuntimeDefinitions
{
    public static void Validate(IEnumerable<CharacterData> characters)
    {
        foreach (var status in Database.StatusEffects.All.Values)
            CombatRules.ValidateStatus(status);
        foreach (var character in characters)
        {
            _ = Combat.CombatLoadout.FromCharacter(character);
            foreach (var action in character.Actions)
                CombatRules.Validate(action, Database.StatusEffects.All);
            foreach (var id in character.StatusEffects)
                if (!Database.StatusEffects.All.ContainsKey(id))
                    throw new InvalidOperationException(
                        $"Character '{character.Id}' references unknown status '{id}'."
                    );
        }
        foreach (var graph in Database.Progression.ProgressionGraphs.All.Values)
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
                CombatRules.Validate(action, Database.StatusEffects.All);
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
    }
}
