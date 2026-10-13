namespace NexusRealms.Prelude.DataModel;

public enum ZodiacSign { Aries, Taurus, Gemini, Cancer, Leo, Virgo, Libra, Scorpio, Sagittarius, Capricorn, Aquarius, Pisces }
/// <summary>Chapter unlock order and screen direction: up, right, down, left.</summary>
public enum ProgressionBranch { North, East, South, West }

/// <summary>Pure authored tree data; positions are relative to the Zodiac at (0, 0).</summary>
public sealed record ProgressionGraph(ZodiacSign Sign, ImmutableArray<ProgressionBranchData> Branches);
public sealed record ProgressionBranchData(ProgressionBranch Direction, ImmutableArray<ProgressionAbilityNode> Abilities);
/// <summary>A null parent connects to the branch root. Coordinates use positive Y down.</summary>
public sealed record ProgressionAbilityNode(AbilityId Ability, AbilityId? Parent, float X, float Y);