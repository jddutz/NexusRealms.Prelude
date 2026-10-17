namespace NexusRealms.Prelude.Actions;

/// <summary>Slows every enemy in the front row for three turns.</summary>
public sealed record WindShearAction : AbilityAction
{
    /// <summary>Creates the implementation bound to Wind Shear's authored data.</summary>
    public WindShearAction() : base("ability:WindShear") { }

    /// <inheritdoc/>
    public override ImmutableArray<string> SelectTargets(CombatActionResolver runtime, string sourceId, string? targetId)
        => base.SelectTargets(runtime, sourceId, targetId).Where(id => runtime.Characters[id].Position
            is FormationSlot.FrontLeft or FormationSlot.FrontCenter or FormationSlot.FrontRight).ToImmutableArray();

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("slow", new(StatusClock.Timeline, 3))];
}
