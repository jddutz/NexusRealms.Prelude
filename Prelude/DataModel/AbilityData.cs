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
    /// <summary>Positive timeline cost; provisional until balanced.</summary>
    public float TurnCost { get; init; } = 1f;
    /// <summary>Focus paid in full at commitment.</summary>
    public int FocusCost { get; init; }
    /// <summary>Focus required to enable the action, distinct from the amount spent.</summary>
    public int MinimumFocus { get; init; }
    /// <summary>Authored recipient selection, evaluated by the action implementation.</summary>
    public TargetingProfile Targeting { get; init; } = new();
}
