namespace NexusRealms.Prelude.DataModel;

public readonly record struct ItemId(string Value)
{
    /// <summary>
    /// Creates a ItemId from a string value.
    /// </summary>
    public static implicit operator ItemId(string value) => new(value);

    /// <summary>
    /// Converts ItemId to its underlying string value.
    /// </summary>
    public static implicit operator string(ItemId id) => id.Value;

    public override string ToString() => Value;

    public static readonly ItemId Invalid = new(string.Empty);

    public static ItemId FromFilePath(string filepath)
    {
        return new ItemId(Path.GetFullPath(filepath));
    }
}
