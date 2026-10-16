namespace NexusRealms.Prelude.Database.Chapter1;

public sealed record Progression : StoryGraph
{
    public Progression()
    {
        Start = "ch1_progression_1";
        End = "ch1_progression_4";
        Nodes =
        [
            new ProgressNode { Id = "ch1_progression_1", Chapter = 1, NextNode = "ch1_progression_2" },
            new ProgressNode { Id = "ch1_progression_2", Chapter = 1, NextNode = "ch1_progression_3" },
            new ProgressNode { Id = "ch1_progression_3", Chapter = 1, NextNode = "ch1_progression_4" },
            new ProgressNode { Id = "ch1_progression_4", Chapter = 1, NextNode = "ch2_progression_1" },
        ];
    }
}