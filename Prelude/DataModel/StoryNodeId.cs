namespace NexusRealms.Prelude.DataModel;

public readonly record struct StoryNodeId(string Value)
{
    /// <summary>
    /// Creates a StoryNodeId from a string value.
    /// </summary>
    public static implicit operator StoryNodeId(string value) => new(value);

    /// <summary>
    /// Converts StoryNodeId to its underlying string value.
    /// </summary>
    public static implicit operator string(StoryNodeId id) => id.Value;

    public override string ToString() => Value;

    public static readonly StoryNodeId Invalid = new(string.Empty);

    public static StoryNodeId FromFilePath(string filepath)
    {
        return new StoryNodeId(Path.GetFullPath(filepath));
    }
}
