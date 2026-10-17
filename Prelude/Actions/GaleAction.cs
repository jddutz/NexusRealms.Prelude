namespace NexusRealms.Prelude.Actions;

/// <summary>Moves all selected enemies one row backward.</summary>
public sealed record GaleAction : AbilityAction
{
    public GaleAction() : base("ability:Gale") { }

    public override ImmutableArray<ActionEffect> Execute() => [new ShiftRowEffect(RowShiftDirection.Backward)];
}
