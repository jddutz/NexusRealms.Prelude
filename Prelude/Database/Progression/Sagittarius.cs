namespace NexusRealms.Prelude.Database.Progression;

public static class Sagittarius
{
    public static ProgressionGraph Graph { get; } =
        new(
            ZodiacSignId.Sagittarius,
            [
                new("north-1", null, -0.84f, -0.84f),
                new("north-2", new AbilityId("north-1"), -0.56f, -0.84f),
                new("north-3", new AbilityId("north-1"), -0.28f, -0.84f),
                new("north-4", new AbilityId("north-3"), 0.0f, -0.84f),
                new("north-5", new AbilityId("north-3"), 0.28f, -0.84f),
                new("east-1", null, -0.84f, -0.28f),
                new("east-2", new AbilityId("east-1"), -0.56f, -0.28f),
                new("east-3", new AbilityId("east-2"), -0.28f, -0.28f),
                new("east-4", new AbilityId("east-2"), 0.0f, -0.28f),
                new("east-5", new AbilityId("east-2"), 0.28f, -0.28f),
                new("south-1", null, -0.84f, 0.28f),
                new("south-2", new AbilityId("south-1"), -0.56f, 0.28f),
                new("south-3", new AbilityId("south-1"), -0.28f, 0.28f),
                new("south-4", new AbilityId("south-1"), 0.0f, 0.28f),
                new("south-5", new AbilityId("south-4"), 0.28f, 0.28f),
                new("west-1", null, -0.84f, 0.84f),
                new("west-2", new AbilityId("west-1"), -0.56f, 0.84f),
                new("west-3", new AbilityId("west-1"), -0.28f, 0.84f),
                new("west-4", new AbilityId("west-3"), 0.0f, -0.84f),
                new("west-5", new AbilityId("west-3"), 0.28f, -0.84f),
            ],
            PlaceholderAbilities.Definitions
        );
}
