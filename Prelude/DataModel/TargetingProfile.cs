namespace NexusRealms.Prelude.DataModel;

/// <summary>Supported fixed recipient selection shapes.</summary>
public enum TargetShape { Single, Self, Row, Column, Adjacent, All }
/// <summary>Action target relationship to its source.</summary>
public enum TargetRelationship { Any, SameTeam, OpposingTeam }
/// <summary>Authored targeting shape and relationship; runtime selection belongs to the resolver.</summary>
public sealed record TargetingProfile(TargetShape Shape = TargetShape.Single, TargetRelationship Relationship = TargetRelationship.Any);
