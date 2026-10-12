namespace NexusRealms.Prelude.DataModel;

/// <summary>Combat action identity, distinct from progression AbilityId.</summary>
public readonly record struct ActionId(string Value)
{
    /// <summary>Creates an action identity from an authored string.</summary>
    public static implicit operator ActionId(string value) => new(value);
    /// <summary>Returns the authored identity.</summary>
    public override string ToString() => Value;
}
