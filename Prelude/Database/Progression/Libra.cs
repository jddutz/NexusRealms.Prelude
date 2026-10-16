namespace NexusRealms.Prelude.Database.Progression;

public static class Libra
{
    public static ProgressionGraph Graph { get; } = new(ZodiacSign.Libra,
    [
        new("north-1", DirectionEnum.North, null, 0.000f, -0.470f),
        new("north-2", DirectionEnum.North, new AbilityId("north-1"), -0.162f, -0.640f),
        new("north-3", DirectionEnum.North, new AbilityId("north-1"), -0.054f, -0.640f),
        new("north-4", DirectionEnum.North, new AbilityId("north-3"), 0.054f, -0.810f),
        new("north-5", DirectionEnum.North, new AbilityId("north-3"), 0.162f, -0.810f),
        new("east-1", DirectionEnum.East, null, 0.470f, 0.000f),
        new("east-2", DirectionEnum.East, new AbilityId("east-1"), 0.640f, -0.162f),
        new("east-3", DirectionEnum.East, new AbilityId("east-2"), 0.810f, -0.054f),
        new("east-4", DirectionEnum.East, new AbilityId("east-2"), 0.810f, 0.054f),
        new("east-5", DirectionEnum.East, new AbilityId("east-2"), 0.810f, 0.162f),
        new("south-1", DirectionEnum.South, null, 0.000f, 0.470f),
        new("south-2", DirectionEnum.South, new AbilityId("south-1"), 0.162f, 0.640f),
        new("south-3", DirectionEnum.South, new AbilityId("south-1"), 0.054f, 0.640f),
        new("south-4", DirectionEnum.South, new AbilityId("south-1"), -0.054f, 0.640f),
        new("south-5", DirectionEnum.South, new AbilityId("south-4"), -0.162f, 0.810f),
        new("west-1", DirectionEnum.West, null, -0.470f, 0.000f),
        new("west-2", DirectionEnum.West, new AbilityId("west-1"), -0.640f, 0.162f),
        new("west-3", DirectionEnum.West, new AbilityId("west-1"), -0.640f, 0.054f),
        new("west-4", DirectionEnum.West, new AbilityId("west-3"), -0.810f, -0.054f),
        new("west-5", DirectionEnum.West, new AbilityId("west-3"), -0.810f, -0.162f),
    ]);
}
