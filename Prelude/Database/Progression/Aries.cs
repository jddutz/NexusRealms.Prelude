namespace NexusRealms.Prelude.Database.Progression;

public static class Aries
{
    public static ProgressionGraph Graph { get; } = new(ZodiacSign.Aries,
    [
        new(ProgressionBranch.North,
        [
                new("north-1", null, 0.000f, -0.470f),
                new("north-2", new AbilityId("north-1"), -0.135f, -0.640f),
                new("north-3", new AbilityId("north-1"), -0.045f, -0.640f),
                new("north-4", new AbilityId("north-3"), 0.045f, -0.810f),
                new("north-5", new AbilityId("north-3"), 0.135f, -0.810f),
        ]),
        new(ProgressionBranch.East,
        [
                new("east-1", null, 0.470f, 0.000f),
                new("east-2", new AbilityId("east-1"), 0.640f, -0.135f),
                new("east-3", new AbilityId("east-2"), 0.810f, -0.045f),
                new("east-4", new AbilityId("east-2"), 0.810f, 0.045f),
                new("east-5", new AbilityId("east-2"), 0.810f, 0.135f),
        ]),
        new(ProgressionBranch.South,
        [
                new("south-1", null, 0.000f, 0.470f),
                new("south-2", new AbilityId("south-1"), 0.135f, 0.640f),
                new("south-3", new AbilityId("south-1"), 0.045f, 0.640f),
                new("south-4", new AbilityId("south-1"), -0.045f, 0.640f),
                new("south-5", new AbilityId("south-4"), -0.135f, 0.810f),
        ]),
        new(ProgressionBranch.West,
        [
                new("west-1", null, -0.470f, 0.000f),
                new("west-2", new AbilityId("west-1"), -0.640f, 0.135f),
                new("west-3", new AbilityId("west-1"), -0.640f, 0.045f),
                new("west-4", new AbilityId("west-3"), -0.810f, -0.045f),
                new("west-5", new AbilityId("west-3"), -0.810f, -0.135f),
        ]),
    ]);
}