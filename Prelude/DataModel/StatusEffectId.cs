namespace NexusRealms.Prelude.DataModel;

public readonly record struct StatusEffectId(string Value)
{
    public static implicit operator StatusEffectId(string value) => new(value);
    public static implicit operator string(StatusEffectId id) => id.Value;
    public override string ToString() => Value;
}
