using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Rules;

/// <summary>Reusable victory predicates and authored objective construction.</summary>
public static class VictoryRules
{
    public static bool AllEnemiesDefeated(CombatSystem combat) => !combat.HasLivingEnemies;

    public static Func<CombatSystem, bool>[] BuildVictoryConditions(this CombatScenario scenario)
    {
        var conditions = scenario.VictoryConditions.Select(ScenarioRules.Build)
            .Concat(scenario.Characters.Select((placement, index) => (placement, index))
                .Where(entry => entry.placement.RequiredForVictory)
                .Select(entry => DefeatRules.CharacterDefeated($"encounter-{entry.index}"))).ToArray();
        return conditions.Length > 0 ? conditions : [AllEnemiesDefeated];
    }

    public static bool IsVictory(CombatSystem combat) => combat.VictoryConditions.Length > 0
        && combat.VictoryConditions.All(condition => condition(combat));
}
