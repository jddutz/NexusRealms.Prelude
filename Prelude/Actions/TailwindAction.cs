namespace NexusRealms.Prelude.Actions;

/// <summary>Moves all selected enemies one row forward.</summary>
public sealed record TailwindAction : AbilityAction
{
    public TailwindAction() : base("ability:Tailwind") { }

    public override ImmutableArray<ActionEffect> Execute() => [new ShiftRowEffect(RowShiftDirection.Forward)];
}
