namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines an immutable character available to story nodes.
/// </summary>
public sealed record CharacterData
{
    /// <summary>
    /// Gets the character's unique identifier.
    /// </summary>
    public required CharacterId Id { get; init; }

    /// <summary>
    /// Gets the character's display name.
    /// </summary>
    public required string Name { get; init; }
    public string[] StatusSymbols { get; init; } = [];
    public int Initiative { get; init; }
    public int Health { get; init; } = 5;
    public int Focus { get; init; } = 2;

    /// <summary>
    /// Gets the identifier of the character's artwork.
    /// </summary>
    public required ContentId Artwork { get; init; }

    /// <summary>Gets the character's turn-order portrait texture.</summary>
    public required ContentId Portrait { get; init; }
}

