using System.Collections.Immutable;

namespace NexusRealms.Prelude.DataModel;

/// <summary>A class-based action composed from explicitly ordered operations.</summary>
public record EffectAction : ActionData
{
    /// <summary>Creates an action without the legacy default attack damage.</summary>
    public EffectAction(string id, params ActionEffect[] effects)
    {
        Id = id;
        Effects = effects.ToImmutableArray();
        Damage = new();
    }
}
