namespace NexusRealms.Prelude.Actions;

/// <summary>Enhances only the caster's normal Focus gains over the next six timeline boundaries.</summary>
public sealed record EyeOfTheStormAction : AbilityAction
{
    public EyeOfTheStormAction() : base("ability:EyeOfTheStorm") { }
    public override ImmutableArray<ActionEffect> Execute() => [new ApplyEffect("eye-of-the-storm", new(StatusClock.Timeline, 6))];
}
