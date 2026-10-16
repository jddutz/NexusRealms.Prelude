namespace NexusRealms.Prelude.DataModel;

/// <summary>Explicit authored ability placements relative to the Zodiac at (0, 0).</summary>
public sealed record ProgressionGraph(ZodiacSign Sign, ImmutableArray<AbilityNode> Abilities);
