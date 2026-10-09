using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude;

/// <summary>UI-owned factories selected by an event's authored presentation key.</summary>
public sealed class TimelineEventRenderer(Func<TimelineOccurrence, bool, Element> fallback)
{
    private readonly Dictionary<string, Func<TimelineOccurrence, bool, Element>> _factories = [];
    public event Action? Changed;

    /// <summary>Registers or replaces a factory. Return a fresh element for each occurrence.</summary>
    public void Register(string key, Func<TimelineOccurrence, bool, Element> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        _factories[key] = factory;
        Changed?.Invoke();
    }

    public Element Create(TimelineOccurrence occurrence, bool isActive = false) =>
        occurrence.PresentationKey is { } key && _factories.TryGetValue(key, out var factory)
            ? factory(occurrence, isActive)
            : fallback(occurrence, isActive);
}
