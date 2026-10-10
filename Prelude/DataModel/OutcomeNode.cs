namespace NexusRealms.Prelude.DataModel;

/// <summary>A placeholder story destination for an encounter outcome.</summary>
public sealed record OutcomeNode : StoryNode
{
    public required string Title { get; init; }
}
