namespace NexusRealms.Prelude.DataModel;

/// <summary>The stable identities used to select zodiac progression graphs.</summary>
public enum ZodiacSignId
{
    Aries,
    Taurus,
    Gemini,
    Cancer,
    Leo,
    Virgo,
    Libra,
    Scorpio,
    Sagittarius,
    Capricorn,
    Aquarius,
    Pisces,
}

/// <summary>The elemental affinity presented during zodiac selection.</summary>
public enum ZodiacElement
{
    Fire,
    Earth,
    Air,
    Water,
}

/// <summary>The modality presented during zodiac selection.</summary>
public enum ZodiacModality
{
    Cardinal,
    Fixed,
    Mutable,
}

/// <summary>Descriptive character-creation data for one zodiac sign.</summary>
public record ZodiacSign
{
    /// <summary>The sign's stable progression identity.</summary>
    public required ZodiacSignId Id { get; init; }
    /// <summary>The display name.</summary>
    public required string Name { get; init; }
    /// <summary>The description displayed during character creation.</summary>
    public required string Description { get; init; }
    /// <summary>The intended style of play.</summary>
    public required string PlayStyle { get; init; }

    /// <summary>The sign's elemental affinity.</summary>
    public required ZodiacElement Element { get; init; }
    /// <summary>The sign's modality.</summary>
    public required ZodiacModality Modality { get; init; }

    /// <summary>The sign's advertised strengths.</summary>
    public required string[] Strengths { get; init; }
    /// <summary>The sign's advertised weaknesses.</summary>
    public required string[] Weaknesses { get; init; }
}
