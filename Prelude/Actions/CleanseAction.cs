namespace NexusRealms.Prelude.Actions;

/// <summary>Shortens every timed negative status on the selected target by one turn.</summary>
public sealed record CleanseAction : AbilityAction
{
    public CleanseAction() : base("ability:Cleanse") { }

    public override ImmutableArray<ActionEffect> Execute() => [new DispelEffect(new(Tag: "negative"), DurationReduction: 1)];
}
