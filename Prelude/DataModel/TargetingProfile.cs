namespace NexusRealms.Prelude.DataModel;

/// <summary>Supported fixed recipient selection shapes.</summary>
public enum TargetShape
{
    /// <summary>Selects one chosen recipient.</summary>
    Single,
    /// <summary>Selects one chosen recipient within the action's melee reach.</summary>
    Melee,
    /// <summary>Selects the source character.</summary>
    Self,
    /// <summary>Selects the chosen recipient's row.</summary>
    Row,
    /// <summary>Selects the chosen recipient's column.</summary>
    Column,
    /// <summary>Selects positions orthogonally adjacent to the chosen recipient.</summary>
    Adjacent,
    /// <summary>Selects positions immediately before or after the chosen recipient in the same formation row.</summary>
    AdjacentInRow,
    /// <summary>Selects every eligible character.</summary>
    All,
    /// <summary>Front-center, both middle-center, and all three back-row positions.</summary>
    Cone,
    /// <summary>Selects every eligible character in the front row.</summary>
    FrontRow,
    /// <summary>Selects every eligible character in the middle row.</summary>
    MiddleRow,
    /// <summary>Selects every eligible character in the back row.</summary>
    BackRow,
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
/// <summary>Explicitly broadens recipient selection beyond the default opposing-team relationship.</summary>
[Flags]
public enum TargetingOptions
{
    /// <summary>Applies the profile's authored team relationship.</summary>
    None = 0,
    /// <summary>Allows recipients from either team, including the source.</summary>
    AllTeams = 1,
}
/// <summary>Authored targeting shape, opposing-team-default relationship, and selection options.</summary>
public sealed record TargetingProfile(
    TargetShape Shape = TargetShape.Single,
    TargetRelationship Relationship = TargetRelationship.OpposingTeam,
    TargetingOptions Options = TargetingOptions.None);
