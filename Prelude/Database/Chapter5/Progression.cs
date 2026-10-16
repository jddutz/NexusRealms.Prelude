namespace NexusRealms.Prelude.Database.Chapter5;

public sealed record Progression : StoryGraph
{
    public Progression()
    {
        Start = "ch5_progression_1";
        End = "ch5_progression_4";
        Nodes =
        [
            new ProgressNode { Id = "ch5_progression_1", Chapter = 5, NextNode = "ch5_progression_2" },
            new ProgressNode { Id = "ch5_progression_2", Chapter = 5, NextNode = "ch5_progression_3" },
            new ProgressNode { Id = "ch5_progression_3", Chapter = 5, NextNode = "ch5_progression_4" },
            new ProgressNode { Id = "ch5_progression_4", Chapter = 5, NextNode = "ch1_p1_Intro" },
        ];
    }
}