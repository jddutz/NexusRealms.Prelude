namespace NexusRealms.Prelude.Actions;

/// <summary>Delays the selected enemy's next action by one and a half turns.</summary>
public sealed record IntimidateAction() : AbilityAction("ability:Intimidate")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [new DelayEffect(1.5f)];
}
