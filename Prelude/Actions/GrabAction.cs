namespace NexusRealms.Prelude.Actions;

/// <summary>Pulls one opponent forward by one formation row.</summary>
public sealed record GrabAction : AbilityAction
{
    /// <summary>Creates the short-range melee grab action.</summary>
    public GrabAction() : base("ability:Grab")
    {
        MeleeRange = WeaponRange.Short;
    }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ShiftRowEffect(RowShiftDirection.Forward)];
}
