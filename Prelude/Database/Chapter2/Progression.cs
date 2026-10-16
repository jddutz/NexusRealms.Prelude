namespace NexusRealms.Prelude.Database.Chapter2;

public sealed record Progression : StoryGraph
{
    public Progression()
    {
        Start = "ch2_progression_1";
        End = "ch2_progression_4";
        Nodes =
        [
            new ProgressNode { Id = "ch2_progression_1", Chapter = 2, NextNode = "ch2_progression_2" },
            new ProgressNode { Id = "ch2_progression_2", Chapter = 2, NextNode = "ch2_progression_3" },
            new ProgressNode { Id = "ch2_progression_3", Chapter = 2, NextNode = "ch2_progression_4" },
            new ProgressNode { Id = "ch2_progression_4", Chapter = 2, NextNode = "ch3_progression_1" },
        ];
    }
}