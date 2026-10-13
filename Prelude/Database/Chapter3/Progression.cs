namespace NexusRealms.Prelude.Database.Chapter3;

public sealed record Progression : StoryGraph
{
    public Progression()
    {
        Start = "ch3_progression_1";
        End = "ch3_progression_4";
        Nodes =
        [
            new ProgressionNode { Id = "ch3_progression_1", Chapter = 3, NextNode = "ch3_progression_2" },
            new ProgressionNode { Id = "ch3_progression_2", Chapter = 3, NextNode = "ch3_progression_3" },
            new ProgressionNode { Id = "ch3_progression_3", Chapter = 3, NextNode = "ch3_progression_4" },
            new ProgressionNode { Id = "ch3_progression_4", Chapter = 3, NextNode = "ch4_progression_1" },
        ];
    }
}