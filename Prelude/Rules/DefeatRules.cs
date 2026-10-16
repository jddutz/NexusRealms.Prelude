using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Rules;

/// <summary>Reusable defeat predicates and authored objective construction.</summary>
public static class DefeatRules
{
    public static Func<CombatSystem, bool> CharacterDefeated(string id) => combat => !combat.IsAlive(id);
    public static bool PlayerDefeated(CombatSystem combat) => !combat.IsAlive("player");
    public static Func<CombatSystem, bool>[] BuildDefeatConditions(this CombatScenario scenario) =>
        [.. scenario.DefeatConditions.Select(ScenarioRules.Build)];
    public static bool IsDefeat(CombatSystem combat) => combat.DefeatConditions.Any(condition => condition(combat));
}
