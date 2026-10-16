namespace NexusRealms.Prelude.DataModel;

/// <summary>Places an authored ability in progression. A null parent connects to the branch root.
/// Coordinates use positive Y down.</summary>
public sealed record AbilityNode(AbilityId Id, DirectionEnum Direction, AbilityId? Parent, float X, float Y);
