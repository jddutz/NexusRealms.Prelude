namespace NexusRealms.Prelude.Database.Progression;

/// <summary>The twelve immutable Zodiac graphs consumed by progression scenes.</summary>
public static class ProgressionGraphs
{
    public static FrozenDictionary<ZodiacSign, ProgressionGraph> All { get; } =
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
