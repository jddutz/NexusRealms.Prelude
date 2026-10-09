namespace NexusRealms.Prelude.Combat;

/// <summary>An authored occurrence; recurring effects schedule their successor in Resolve.</summary>
public sealed record EncounterEvent(float Turn, string Label, Action<CombatSystem> Resolve,
    int Priority = TimelinePriority.Effect, string? Icon = null,
    bool IsVisible = true, string? PresentationKey = null);

