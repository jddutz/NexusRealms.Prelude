using NexusRealms.Prelude.Database.Chapter1;
using NexusRealms.Prelude.Database.Chapter2;
using NexusRealms.Prelude.Database.Chapter3;
using NexusRealms.Prelude.Database.Chapter4;
using NexusRealms.Prelude.Database.Chapter5;

namespace NexusRealms.Prelude.Database;

/// <summary>
/// Provides the complete Prelude story graph.
/// </summary>
public sealed class Storyline : IStoryGraph
{
    public IStoryGraph? Parent => null;

    public IStoryGraph Chapter1 { get; }
    public IStoryGraph Chapter2 { get; }
    public IStoryGraph Chapter3 { get; }
    public IStoryGraph Chapter4 { get; }
    public IStoryGraph Chapter5 { get; }

    public IEnumerable<CharacterData> Characters =>
        [
            .. Chapter1.Characters,
            .. Chapter2.Characters,
            .. Chapter3.Characters,
            .. Chapter4.Characters,
            .. Chapter5.Characters,
        ];

    public IEnumerable<StoryNode> Nodes =>
        [
            .. Chapter1.Nodes,
            .. Chapter2.Nodes,
            .. Chapter3.Nodes,
            .. Chapter4.Nodes,
            .. Chapter5.Nodes,
        ];

    public StoryNodeId Start => Chapter1.Start;
    public StoryNodeId End => Chapter5.End;

    public Storyline()
    {
        Chapter1 = new Chapter1StoryGraph(this);
        Chapter2 = new Chapter2StoryGraph(this);
        Chapter3 = new Chapter3StoryGraph(this);
        Chapter4 = new Chapter4StoryGraph(this);
        Chapter5 = new Chapter5StoryGraph(this);
    }
}
