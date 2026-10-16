namespace NexusRealms.Prelude.Database;

/// <summary>Authored ability definitions and their runtime lookup.</summary>
public static class Abilities
{
    public static AbilityData Slash { get; } =
        new()
        {
            Id = "slash",
            Name = "Slash",
            Description = "Strike with a sweeping blade.",
            Icon = new("stats.swords_crossed.png"),
        };

    public static AbilityData Push { get; } =
        new()
        {
            Id = "push",
            Name = "Push",
            Description = "Shove an opponent away.",
            Icon = new("actions.fist.png"),
        };

    public static AbilityData Parry { get; } =
        new()
        {
            Id = "parry",
            Name = "Parry",
            Description = "Deflect an incoming strike.",
            Icon = new("stats.shield_glimmer.png"),
        };

    // Shared authored placeholders referenced by the other Zodiac layouts.
    public static AbilityData North1 { get; } = new()
    {
        Id = "north-1", Name = "north-1",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData North2 { get; } = new()
    {
        Id = "north-2", Name = "north-2",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData North3 { get; } = new()
    {
        Id = "north-3", Name = "north-3",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData North4 { get; } = new()
    {
        Id = "north-4", Name = "north-4",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData North5 { get; } = new()
    {
        Id = "north-5", Name = "north-5",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData East1 { get; } = new()
    {
        Id = "east-1", Name = "east-1",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData East2 { get; } = new()
    {
        Id = "east-2", Name = "east-2",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData East3 { get; } = new()
    {
        Id = "east-3", Name = "east-3",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData East4 { get; } = new()
    {
        Id = "east-4", Name = "east-4",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData East5 { get; } = new()
    {
        Id = "east-5", Name = "east-5",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData South1 { get; } = new()
    {
        Id = "south-1", Name = "south-1",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData South2 { get; } = new()
    {
        Id = "south-2", Name = "south-2",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData South3 { get; } = new()
    {
        Id = "south-3", Name = "south-3",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData South4 { get; } = new()
    {
        Id = "south-4", Name = "south-4",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData South5 { get; } = new()
    {
        Id = "south-5", Name = "south-5",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData West1 { get; } = new()
    {
        Id = "west-1", Name = "west-1",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData West2 { get; } = new()
    {
        Id = "west-2", Name = "west-2",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData West3 { get; } = new()
    {
        Id = "west-3", Name = "west-3",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData West4 { get; } = new()
    {
        Id = "west-4", Name = "west-4",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };
    public static AbilityData West5 { get; } = new()
    {
        Id = "west-5", Name = "west-5",
        Description = "Progression placeholder; combat effects have not been authored.",
        Icon = new("stats.swords_crossed.png"),
    };

    /// <summary>Lookup of explicitly authored records; duplicate IDs fail registration.</summary>
    public static FrozenDictionary<AbilityId, AbilityData> All { get; } =
        new AbilityData[]
        {
            Slash, Push, Parry,
            North1, North2, North3, North4, North5,
            East1, East2, East3, East4, East5,
            South1, South2, South3, South4, South5,
            West1, West2, West3, West4, West5,
        }.Concat(Progression.Aquarius.Definitions).ToFrozenDictionary(ability => ability.Id);
}
