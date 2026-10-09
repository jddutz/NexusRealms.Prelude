namespace NexusRealms.Prelude.Database;

/// <summary>Status definitions shared by detailed descriptions and compact HUD icons.</summary>
public static class StatusEffects
{
    public static readonly StatusEffectData Guarded = new()
    {
        Id = "guarded", Name = "Guarded", Description = "A protective status effect.",
        Icon = new("stats.shield.png"),
    };
    public static readonly StatusEffectData Inspired = new()
    {
        Id = "inspired", Name = "Inspired", Description = "An encouraging status effect.",
        Icon = new("stats.arrow_up.png"),
    };
    public static readonly StatusEffectData Focused = new()
    {
        Id = "focused", Name = "Focused", Description = "A concentration status effect.",
        Icon = new("stats.moon_star.png"),
    };
    public static FrozenDictionary<StatusEffectId, StatusEffectData> All { get; } =
        new[] { Guarded, Inspired, Focused }.ToFrozenDictionary(effect => effect.Id);
}
