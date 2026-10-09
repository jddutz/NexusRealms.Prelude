namespace NexusRealms.Prelude.DataModel;

/// <summary>Authored presentation for a status effect; combat behavior is defined separately.</summary>
public sealed record StatusEffectData
{
    public required StatusEffectId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ContentId Icon { get; init; }
}
