using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Rules;

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
    /// <summary>Resolves supported objective definitions without storing runtime delegates in data records.</summary>
    internal static Func<CombatSystem, bool> Build(CombatConditionData condition) =>
        condition.Kind switch
        {
            CombatConditionKind.PlayerDefeated => DefeatRules.PlayerDefeated,
            CombatConditionKind.AllEnemiesDefeated => VictoryRules.AllEnemiesDefeated,
            CombatConditionKind.CharacterDefeated
                when !string.IsNullOrWhiteSpace(condition.CharacterId) =>
                DefeatRules.CharacterDefeated(condition.CharacterId),
            _ => throw new ArgumentException("Invalid encounter objective."),
        };
}
