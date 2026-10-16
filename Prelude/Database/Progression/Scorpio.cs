namespace NexusRealms.Prelude.Database.Progression;

public static class Scorpio
{
    public static ProgressionGraph Graph { get; } = ProgressionGraph.FromBranches(ZodiacSign.Scorpio,
    [
        new(ProgressionBranch.North,
        [
                new("north-1", null, 0.000f, -0.470f),
                new("north-2", new AbilityId("north-1"), -0.167f, -0.640f),
                new("north-3", new AbilityId("north-2"), -0.056f, -0.810f),
                new("north-4", new AbilityId("north-2"), 0.056f, -0.810f),
                new("north-5", new AbilityId("north-2"), 0.167f, -0.810f),
        ]),
        new(ProgressionBranch.East,
        [
                new("east-1", null, 0.470f, 0.000f),
                new("east-2", new AbilityId("east-1"), 0.640f, -0.167f),
                new("east-3", new AbilityId("east-1"), 0.640f, -0.056f),
                new("east-4", new AbilityId("east-1"), 0.640f, 0.056f),
                new("east-5", new AbilityId("east-4"), 0.810f, 0.167f),
        ]),
        new(ProgressionBranch.South,
        [
                new("south-1", null, 0.000f, 0.470f),
                new("south-2", new AbilityId("south-1"), 0.167f, 0.640f),
                new("south-3", new AbilityId("south-1"), 0.056f, 0.640f),
                new("south-4", new AbilityId("south-3"), -0.056f, 0.810f),
                new("south-5", new AbilityId("south-3"), -0.167f, 0.810f),
        ]),
        new(ProgressionBranch.West,
        [
                new("west-1", null, -0.470f, 0.000f),
                new("west-2", new AbilityId("west-1"), -0.640f, 0.167f),
                new("west-3", new AbilityId("west-2"), -0.810f, 0.056f),
                new("west-4", new AbilityId("west-2"), -0.810f, -0.056f),
                new("west-5", new AbilityId("west-2"), -0.810f, -0.167f),
        ]),
    ]);
}