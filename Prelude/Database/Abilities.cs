namespace NexusRealms.Prelude.Database;

/// <summary>Ability presentation placeholders; these do not grant executable combat commands.</summary>
public static class Abilities
{
    public static readonly AbilityData Slash = new()
    {
        Id = "slash", Name = "Slash", Description = "Strike with a sweeping blade.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static readonly AbilityData Push = new()
    {
        Id = "push", Name = "Push", Description = "Shove an opponent away.",
        Icon = new("actions.fist.png"),
    };
    public static readonly AbilityData Parry = new()
    {
        Id = "parry", Name = "Parry", Description = "Deflect an incoming strike.",
        Icon = new("stats.shield_glimmer.png"),
    };
    public static readonly AbilityData BladeStorm = new()
    {
        Id = "blade-storm", Name = "Blade Storm", Description = "Unleash a flurry of blade strikes.",
        Icon = new("stats.arrows_circle_up.png"),
    };
    public static IReadOnlyList<AbilityData> DisplayOrder { get; } =
        Array.AsReadOnly(new[] { Slash, Push, Parry, BladeStorm });
    public static FrozenDictionary<AbilityId, AbilityData> All { get; } =
        DisplayOrder.Concat(Enum.GetValues<ProgressionBranch>().SelectMany(branch =>
            Enumerable.Range(1, 5).Select(index => new AbilityData
            {
                Id = $"{branch.ToString().ToLowerInvariant()}-{index}",
                Name = $"{branch} {index}",
                Description = "Progression placeholder; combat effects have not been authored.",
                Icon = new("stats.swords_crossed.png"),
            }))).ToFrozenDictionary(ability => ability.Id);
}
