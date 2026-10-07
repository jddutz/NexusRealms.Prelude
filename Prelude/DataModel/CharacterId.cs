namespace NexusRealms.Prelude.DataModel;

public readonly record struct CharacterId(string Value)
{
    /// <summary>
    /// Creates a CharacterId from a string value.
    /// </summary>
    public static implicit operator CharacterId(string value) => new(value);

    /// <summary>
    /// Converts CharacterId to its underlying string value.
    /// </summary>
    public static implicit operator string(CharacterId id) => id.Value;

    public override string ToString() => Value;

    public static readonly CharacterId Invalid = new(string.Empty);

    public static CharacterId FromFilePath(string filepath)
    {
        return new CharacterId(Path.GetFullPath(filepath));
    }
}
