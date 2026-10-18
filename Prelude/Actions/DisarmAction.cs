using System.Collections.Immutable;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Actions;

/// <summary>Disarms an opponent by moving their equipped weapons into inventory temporarily.</summary>
public sealed record DisarmAction : AbilityAction
{
    /// <summary>Creates the Disarm action.</summary>
    public DisarmAction() : base("ability:Disarm") { }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [new RemoveEffect(new DisarmEquipment())];
}
