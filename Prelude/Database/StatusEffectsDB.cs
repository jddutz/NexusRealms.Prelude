namespace NexusRealms.Prelude.Database;

/// <summary>Status definitions shared by detailed descriptions and compact HUD icons.</summary>
public class StatusEffects
{
    public static StatusEffectData Guarded =>
        new()
        {
            Id = "guarded",
            Name = "Guarded",
            Description = "A protective status effect.",
            Duplicates = DuplicateStatusRule.Refresh,
            Icon = new("stats.shield.png"),
        };

    public static StatusEffectData Inspired =>
        new()
        {
            Id = "inspired",
            Name = "Inspired",
            Description = "An encouraging status effect.",
            Duplicates = DuplicateStatusRule.Refresh,
            Icon = new("stats.arrow_up.png"),
        };

    public static StatusEffectData Focused =>
        new()
        {
            Id = "focused",
            Name = "Focused",
            Description = "A concentration status effect.",
            Duplicates = DuplicateStatusRule.Refresh,
            Icon = new("stats.moon_star.png"),
        };

    public static FrozenDictionary<StatusEffectId, StatusEffectData> All { get; } =
        new[] { Guarded, Inspired, Focused }.ToFrozenDictionary(status => status.Id);
}
