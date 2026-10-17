using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>A story node displaying authored narrative text and branching choices.</summary>
public sealed record NarrativeNode : StoryNode
{
    /// <summary>Gets the background image displayed behind the narrative.</summary>
    public required ContentId BackgroundImage { get; init; }

    /// <summary>Gets the foreground illustration shown with the narrative.</summary>
    public required ContentId ForegroundImage { get; init; }

    /// <summary>Gets the BBCode-like narrative copy rendered by the narrative scene.</summary>
    public required string RichText { get; init; }

    /// <summary>Gets the choices that navigate to subsequent story nodes.</summary>
    public required ImmutableArray<NarrativeChoice> Choices { get; init; }
}

/// <summary>A labeled choice that routes the story to another node.</summary>
public sealed record NarrativeChoice
{
    /// <summary>Gets the label shown to the player.</summary>
    public required string Text { get; init; }

    /// <summary>Gets the identifier of the node selected by this choice.</summary>
    public required StoryNodeId NextNode { get; init; }
}
