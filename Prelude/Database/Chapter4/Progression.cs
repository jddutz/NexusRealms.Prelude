namespace NexusRealms.Prelude.Database.Chapter4;

public sealed record Progression : StoryGraph
{
    public Progression()
    {
        Start = "ch4_progression_1";
        End = "ch4_progression_4";
        Nodes =
        [
            new ProgressionNode { Id = "ch4_progression_1", Chapter = 4, NextNode = "ch4_progression_2" },
            new ProgressionNode { Id = "ch4_progression_2", Chapter = 4, NextNode = "ch4_progression_3" },
            new ProgressionNode { Id = "ch4_progression_3", Chapter = 4, NextNode = "ch4_progression_4" },
            new ProgressionNode { Id = "ch4_progression_4", Chapter = 4, NextNode = "ch5_progression_1" },
        ];
    }
}