namespace NexusRealms.Prelude.Database.Progression;

/// <summary>Temporary shared abilities used until the remaining Zodiac definitions are authored.</summary>
public static class PlaceholderAbilities
{
    public static AbilityData Slash { get; } =
        new()
        {
            Id = "slash",
            ActionId = "ability:slash",
            Name = "Slash",
            Description = "Strike with a sweeping blade.",
            Icon = new("abilities.Attack.png"),
        };

    public static AbilityData Push { get; } =
        new()
        {
            Id = "push",
            ActionId = "ability:push",
            Name = "Push",
            Description = "Shove an opponent away.",
            Icon = new("abilities.Attack.png"),
        };

    public static AbilityData Parry { get; } =
        new()
        {
            Id = "parry",
            ActionId = "ability:parry",
            Name = "Parry",
            Description = "Deflect an incoming strike.",
            Icon = new("abilities.Shield.png"),
        };

    // Shared authored placeholders referenced by the other Zodiac layouts.
    public static AbilityData North1 { get; } =
        new()
        {
            Id = "north-1",
            ActionId = "ability:north-1",
            Name = "north-1",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData North2 { get; } =
        new()
        {
            Id = "north-2",
            ActionId = "ability:north-2",
            Name = "north-2",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData North3 { get; } =
        new()
        {
            Id = "north-3",
            ActionId = "ability:north-3",
            Name = "north-3",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData North4 { get; } =
        new()
        {
            Id = "north-4",
            ActionId = "ability:north-4",
            Name = "north-4",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData North5 { get; } =
        new()
        {
            Id = "north-5",
            ActionId = "ability:north-5",
            Name = "north-5",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData East1 { get; } =
        new()
        {
            Id = "east-1",
            ActionId = "ability:east-1",
            Name = "east-1",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData East2 { get; } =
        new()
        {
            Id = "east-2",
            ActionId = "ability:east-2",
            Name = "east-2",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData East3 { get; } =
        new()
        {
            Id = "east-3",
            ActionId = "ability:east-3",
            Name = "east-3",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData East4 { get; } =
        new()
        {
            Id = "east-4",
            ActionId = "ability:east-4",
            Name = "east-4",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData East5 { get; } =
        new()
        {
            Id = "east-5",
            ActionId = "ability:east-5",
            Name = "east-5",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData South1 { get; } =
        new()
        {
            Id = "south-1",
            ActionId = "ability:south-1",
            Name = "south-1",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData South2 { get; } =
        new()
        {
            Id = "south-2",
            ActionId = "ability:south-2",
            Name = "south-2",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData South3 { get; } =
        new()
        {
            Id = "south-3",
            ActionId = "ability:south-3",
            Name = "south-3",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData South4 { get; } =
        new()
        {
            Id = "south-4",
            ActionId = "ability:south-4",
            Name = "south-4",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData South5 { get; } =
        new()
        {
            Id = "south-5",
            ActionId = "ability:south-5",
            Name = "south-5",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData West1 { get; } =
        new()
        {
            Id = "west-1",
            ActionId = "ability:west-1",
            Name = "west-1",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData West2 { get; } =
        new()
        {
            Id = "west-2",
            ActionId = "ability:west-2",
            Name = "west-2",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData West3 { get; } =
        new()
        {
            Id = "west-3",
            ActionId = "ability:west-3",
            Name = "west-3",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData West4 { get; } =
        new()
        {
            Id = "west-4",
            ActionId = "ability:west-4",
            Name = "west-4",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };
    public static AbilityData West5 { get; } =
        new()
        {
            Id = "west-5",
            ActionId = "ability:west-5",
            Name = "west-5",
            Description = "Progression placeholder; combat effects have not been authored.",
            Icon = new("abilities.Attack.png"),
        };

    public static ImmutableArray<AbilityData> Definitions { get; } =
    [
        Slash,
        Push,
        Parry,
        North1,
        North2,
        North3,
        North4,
        North5,
        East1,
        East2,
        East3,
        East4,
        East5,
        South1,
        South2,
        South3,
        South4,
        South5,
        West1,
        West2,
        West3,
        West4,
        West5,
    ];
}
