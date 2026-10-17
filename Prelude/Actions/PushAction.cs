namespace NexusRealms.Prelude.Actions;

/// <summary>Pushes one opponent back by one formation row.</summary>
public sealed record PushAction : AbilityAction
{
    /// <summary>Creates the short-range melee push action.</summary>
    public PushAction() : base("ability:Push")
    {
        MeleeRange = WeaponRange.Short;
    }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ShiftRowEffect(RowShiftDirection.Backward)];
}
