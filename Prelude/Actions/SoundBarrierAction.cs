namespace NexusRealms.Prelude.Actions;

/// <summary>Prevents enemies in the back row from using magical actions for two timeline turns.</summary>
public sealed record SoundBarrierAction : AbilityAction
{
    /// <summary>Creates the implementation bound to Sound Barrier's authored data.</summary>
    public SoundBarrierAction() : base("ability:SoundBarrier") { }

    /// <inheritdoc/>
    public override ImmutableArray<string> SelectTargets(CombatActionResolver runtime, string sourceId, string? targetId)
        => base.SelectTargets(runtime, sourceId, targetId).Where(id => runtime.Characters[id].Position
            is FormationSlot.BackLeft or FormationSlot.BackCenter or FormationSlot.BackRight).ToImmutableArray();

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("silenced", new(StatusClock.Timeline, 2))];
}
