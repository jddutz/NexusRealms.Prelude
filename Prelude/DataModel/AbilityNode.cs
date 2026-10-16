namespace NexusRealms.Prelude.DataModel;

/// <summary>Places an authored ability on its Zodiac artwork. Coordinates range from -1 to 1:
/// (-1,-1) is the cropped artwork's top-left, (0,0) its center, and (1,1) its bottom-right.
/// A null parent means no prerequisite connection.</summary>
public sealed record AbilityNode(AbilityId Id, AbilityId? Parent, float X, float Y);
