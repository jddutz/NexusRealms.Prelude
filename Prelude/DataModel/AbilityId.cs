namespace NexusRealms.Prelude.DataModel;

public readonly record struct AbilityId(string Value)
{
    /// <summary>
    /// Creates a AbilityId from a string value.
    /// </summary>
    public static implicit operator AbilityId(string value) => new(value);

    /// <summary>
    /// Converts AbilityId to its underlying string value.
    /// </summary>
    public static implicit operator string(AbilityId id) => id.Value;

    public override string ToString() => Value;

    public static readonly AbilityId Invalid = new(string.Empty);

    public static AbilityId FromFilePath(string filepath)
    {
        return new AbilityId(Path.GetFullPath(filepath));
    }
}
