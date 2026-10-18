namespace NexusRealms.Prelude.DataModel;

/// <summary>Data-only progression unlock referencing one runtime action implementation.</summary>
public sealed record AbilityData
{
    public required AbilityId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ContentId Icon { get; init; }
    /// <summary>The single action unlocked by learning this ability.</summary>
    public required ActionId ActionId { get; init; }
    /// <summary>Activation timeline cost, or null when the ability is passive.</summary>
    public float? TurnCost { get; init; }
    /// <summary>Fixed effect duration in timeline turns, or null when it has no fixed turn duration.</summary>
    public float? Duration { get; init; }
    /// <summary>Focus paid at activation, or null when the ability is passive.</summary>
    public int? FocusCost { get; init; }
    /// <summary>Focus required to enable the action, distinct from the amount spent.</summary>
    public int MinimumFocus { get; init; }
    /// <summary>Whether this learned ability is not manually activated as a combat action.</summary>
    public bool Passive { get; init; }
    /// <summary>Properties of the unlocked action, independent of its target selection.</summary>
    public ActionFlags Flags { get; init; }
    /// <summary>Activation recipient selection, absent for passive abilities.</summary>
    public TargetingProfile? Targeting { get; init; }
}
