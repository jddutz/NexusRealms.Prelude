using System.ComponentModel;

namespace NexusRealms.Prelude.DataModel;

public enum ZodiacSign
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

/// <summary>Pure authored tree data; positions are relative to the Zodiac at (0, 0).</summary>
public sealed record ProgressionGraph(ZodiacSign Sign, ImmutableArray<AbilityNode> Abilities)
{
    /// <summary>Directional view used by chapter unlocks and progression presentation.</summary>
    public ImmutableArray<ProgressionBranchData> Branches => Enum.GetValues<ProgressionBranch>()
        .Select(direction => new ProgressionBranchData(direction, Abilities
            .Where(node => (int)node.Direction == (int)direction)
            .Select(node => new ProgressionAbilityNode(node.Ability, node.Parent, node.X, node.Y))
            .ToImmutableArray())).ToImmutableArray();

    /// <summary>Adapts Zodiac layouts that still use branch-based placeholder authoring.</summary>
    public static ProgressionGraph FromBranches(ZodiacSign sign, ImmutableArray<ProgressionBranchData> branches) =>
        new(sign, branches.SelectMany(branch => branch.Abilities.Select(node => new AbilityNode(
            node.Ability.Value, "Progression placeholder; combat effects have not been authored.",
            (DirectionEnum)branch.Direction, node.Ability, node.Parent, node.X, node.Y))).ToImmutableArray());
}

/// <summary>Chapter unlock order used by the existing progression presentation.</summary>
public enum ProgressionBranch { North, East, South, West }
public sealed record ProgressionBranchData(ProgressionBranch Direction, ImmutableArray<ProgressionAbilityNode> Abilities);
public sealed record ProgressionAbilityNode(AbilityId Ability, AbilityId? Parent, float X, float Y);

/// <summary>A null parent connects to the branch root. Coordinates use positive Y down.</summary>
public sealed record AbilityNode(
    string DisplayName,
    string Description,
    DirectionEnum Direction,
    AbilityId Ability,
    AbilityId? Parent,
    float X,
    float Y
);
