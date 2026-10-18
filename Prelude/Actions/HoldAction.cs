namespace NexusRealms.Prelude.Actions;

/// <summary>Holds one nearby opponent in place and prevents their Melee actions for two timeline turns.</summary>
public sealed record HoldAction : AbilityAction
{
    /// <summary>Creates the short-range Hold action.</summary>
    public HoldAction() : base("ability:Hold")
    {
        MeleeRange = WeaponRange.Short;
    }

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() =>
        [new ApplyEffect("held", new(StatusClock.Timeline, 2))];
}
