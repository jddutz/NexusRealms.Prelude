namespace NexusRealms.Prelude.Actions;

/// <summary>Consumes all Focus and restores two Health per Focus spent.</summary>
public sealed record RecoveryAction() : AbilityAction("ability:Recovery")
{
    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute() => [];

    /// <inheritdoc/>
    public override ImmutableArray<ActionEffect> Execute(CombatActionResolver runtime, string sourceId)
    {
        var loadout = runtime.Characters[sourceId].Loadout;
        var focusCost = Costs
            .Where(cost => cost.Subject is ResourceSubject { Resource: ResourceKind.Focus })
            .Sum(cost => (long)cost.Amount);
        var totalFocus = loadout.Focus + focusCost;
        var healthRestored = (int)Math.Min(int.MaxValue, totalFocus * 2);

        return
        [
            new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), loadout.Focus)
            {
                Recipients = RecipientScope.Source,
            },
            new RestoreEffect(healthRestored)
            {
                Recipients = RecipientScope.Source,
            },
        ];
    }
}
