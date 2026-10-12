using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude;

/// <summary>Runtime adapters for data-only scenario objectives.</summary>
public static class ScenarioRules
{
    public static void ConfigureCombat(this CombatScenario scenario, CombatSystem combat)
    {
        var victory = scenario.VictoryConditions.Concat(scenario.Characters.Select((placement, index) => (placement, index))
            .Where(entry => entry.placement.RequiredForVictory)
            .Select(entry => new CombatConditionData(CombatConditionKind.CharacterDefeated, $"encounter-{entry.index}"))).ToArray();
        combat.ConfigureObjectives(victory.Length == 0 ? [new(CombatConditionKind.AllEnemiesDefeated)] : victory, scenario.DefeatConditions);
    }
    /// <summary>Combines explicit objectives and designated character objectives into runtime predicates.</summary>
    public static Func<CombatSystem, bool>[] BuildVictoryConditions(this CombatScenario scenario)
    {
        var conditions = scenario
            .VictoryConditions.Select(Build)
            .Concat(
                scenario
                    .Characters.Select((placement, index) => (placement, index))
                    .Where(entry => entry.placement.RequiredForVictory)
                    .Select(entry => CombatConditions.CharacterDefeated($"encounter-{entry.index}"))
            )
            .ToArray();
        return conditions.Length > 0 ? conditions : [CombatConditions.AllEnemiesDefeated];
    }

    /// <summary>Builds runtime defeat predicates from authored objective data.</summary>
    public static Func<CombatSystem, bool>[] BuildDefeatConditions(this CombatScenario scenario) =>
        [.. scenario.DefeatConditions.Select(Build)];

    /// <summary>Resolves supported objective definitions without storing runtime delegates in data records.</summary>
    internal static Func<CombatSystem, bool> Build(CombatConditionData condition) =>
        condition.Kind switch
        {
            CombatConditionKind.PlayerDefeated => CombatConditions.PlayerDefeated,
            CombatConditionKind.AllEnemiesDefeated => CombatConditions.AllEnemiesDefeated,
            CombatConditionKind.CharacterDefeated
                when !string.IsNullOrWhiteSpace(condition.CharacterId) =>
                CombatConditions.CharacterDefeated(condition.CharacterId),
            _ => throw new ArgumentException("Invalid encounter objective."),
        };
}
