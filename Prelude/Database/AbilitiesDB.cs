namespace NexusRealms.Prelude.Database;

/// <summary>Ability presentation placeholders; these do not grant executable combat commands.</summary>
public static class Abilities
{
    public static AbilityData Slash =>
        new()
        {
            Id = "slash",
            Name = "Slash",
            Description = "Strike with a sweeping blade.",
            Icon = new("stats.swords_crossed.png"),
        };

    public static AbilityData Push =>
        new()
        {
            Id = "push",
            Name = "Push",
            Description = "Shove an opponent away.",
            Icon = new("actions.fist.png"),
        };

    public static AbilityData Parry =>
        new()
        {
            Id = "parry",
            Name = "Parry",
            Description = "Deflect an incoming strike.",
            Icon = new("stats.shield_glimmer.png"),
        };

    /// <summary>All progression nodes have presentation definitions; actions are added one ability at a time.</summary>
    public static FrozenDictionary<AbilityId, AbilityData> All { get; } = BuildRegistry();

    private static FrozenDictionary<AbilityId, AbilityData> BuildRegistry()
    {
        var definitions = new Dictionary<AbilityId, AbilityData>();
        foreach (var ability in new[] { Slash, Push, Parry }) definitions.Add(ability.Id, ability);
        foreach (var node in Progression.ProgressionGraphs.All.Values.SelectMany(graph => graph.Abilities))
        {
            var definition = new AbilityData
            {
                Id = node.Ability, Name = node.DisplayName, Description = node.Description,
                Icon = new("stats.swords_crossed.png"),
            };
            if (definitions.TryGetValue(node.Ability, out var existing))
            {
                if (existing.Name != definition.Name || existing.Description != definition.Description)
                    throw new InvalidOperationException($"Conflicting ability definition '{node.Ability}'.");
            }
            else definitions.Add(definition.Id, definition);
        }
        foreach (var ability in Progression.Aquarius.Definitions)
        {
            if (!Progression.Aquarius.Graph.Abilities.Any(node => node.Ability == ability.Id))
                throw new InvalidOperationException($"Authored Aquarius ability '{ability.Id}' has no progression node.");
            definitions[ability.Id] = ability;
        }
        return definitions.ToFrozenDictionary();
    }
}
