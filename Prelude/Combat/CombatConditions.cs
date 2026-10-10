namespace NexusRealms.Prelude.Combat;

/// <summary>Reusable predicates for scenario-authored outcome arrays.</summary>
public static class CombatConditions
{
    public static Func<CombatSystem, bool> CharacterDefeated(string id) => combat => !combat.IsAlive(id);
    public static bool AllEnemiesDefeated(CombatSystem combat) => !combat.HasLivingEnemies;
    public static bool PlayerDefeated(CombatSystem combat) => !combat.IsAlive("player");
}
