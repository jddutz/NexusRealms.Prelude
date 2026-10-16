namespace NexusRealms.Prelude.Database.Progression;

/// <summary>The twelve immutable Zodiac graphs consumed by progression scenes.</summary>
public static class ProgressionGraphs
{
    /// <summary>The authored progression graph for each stable zodiac identity.</summary>
    public static FrozenDictionary<ZodiacSignId, ProgressionGraph> All { get; } =
        new[]
        {
            Aries.Graph,
            Taurus.Graph,
            Gemini.Graph,
            Cancer.Graph,
            Leo.Graph,
            Virgo.Graph,
            Libra.Graph,
            Scorpio.Graph,
            Sagittarius.Graph,
            Capricorn.Graph,
            Aquarius.Graph,
            Pisces.Graph,
        }.ToFrozenDictionary(graph => graph.Sign);
}
