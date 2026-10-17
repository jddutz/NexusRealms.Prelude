namespace NexusRealms.Prelude.DataModel;

/// <summary>Supported fixed recipient selection shapes.</summary>
public enum TargetShape
{
    /// <summary>Selects one chosen recipient.</summary>
    Single,
    /// <summary>Selects the source character.</summary>
    Self,
    /// <summary>Selects the chosen recipient's row.</summary>
    Row,
    /// <summary>Selects the chosen recipient's column.</summary>
    Column,
    /// <summary>Selects positions orthogonally adjacent to the chosen recipient.</summary>
    Adjacent,
    /// <summary>Selects every eligible character.</summary>
    All,
    /// <summary>Front-center, both middle-center, and all three back-row positions.</summary>
    Cone,
}
/// <summary>Action target relationship to its source.</summary>
public enum TargetRelationship
{
    /// <summary>Does not filter recipients by team.</summary>
    Any,
    /// <summary>Restricts recipients to the source's team.</summary>
    SameTeam,
    /// <summary>Restricts recipients to the opposing team.</summary>
    OpposingTeam,
}
/// <summary>Explicit action requirements and affinities; combine requirements with bitwise OR.</summary>
[Flags]
public enum TargetingFlags
{
    /// <summary>No explicit requirements or affinities.</summary>
    None = 0,
    /// <summary>Requires sight and is prevented by Blind.</summary>
    Visual = 1,
    /// <summary>Has magical affinity and is prevented by Silence, independently of speech.</summary>
    Magical = 2,
}
/// <summary>Explicitly broadens recipient selection beyond the default opposing-team relationship.</summary>
[Flags]
public enum TargetingOptions
{
    /// <summary>Applies the profile's authored team relationship.</summary>
    None = 0,
    /// <summary>Allows recipients from either team, including the source.</summary>
    AllTeams = 1,
}
/// <summary>Authored targeting shape, opposing-team-default relationship, requirements, and selection options.</summary>
public sealed record TargetingProfile(
    TargetShape Shape = TargetShape.Single,
    TargetRelationship Relationship = TargetRelationship.OpposingTeam,
    TargetingFlags Flags = TargetingFlags.None,
    TargetingOptions Options = TargetingOptions.None);
