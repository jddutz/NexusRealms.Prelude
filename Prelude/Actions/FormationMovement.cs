namespace NexusRealms.Prelude.Actions;

/// <summary>Deterministic one-row movement through the staggered 3-4-3 formation.</summary>
internal static class FormationMovement
{
    // Move the leading row first so following recipients can use vacated positions.
    // Freeze this order before mutation; each recipient is still processed exactly once.
    public static ImmutableArray<string> Order(CombatActionResolver runtime, IEnumerable<string> recipients, RowShiftDirection direction)
        => recipients.OrderBy(id => runtime.Characters[id].Position is { } slot
                ? TargetSelection.Coordinates(slot).Row * (direction == RowShiftDirection.Backward ? 1 : -1) : int.MaxValue)
            .ThenBy(id => runtime.Characters[id].Position is { } slot ? TargetSelection.Coordinates(slot).Column : int.MaxValue)
            .ThenBy(id => id, StringComparer.Ordinal).ToImmutableArray();

    public static FormationSlot? Destination(CombatActionResolver runtime, ActionCharacterState recipient, RowShiftDirection direction)
    {
        if (recipient.IsPlayer || recipient.Position is not { } origin) return null;
        var (row, column) = TargetSelection.Coordinates(origin);
        var destinationRow = row + (direction == RowShiftDirection.Backward ? -1 : 1);
        return Enum.GetValues<FormationSlot>()
            .Where(slot => TargetSelection.Coordinates(slot).Row == destinationRow)
            .OrderBy(slot => Math.Abs(TargetSelection.Coordinates(slot).Column - column))
            .ThenBy(slot => slot)
            .Where(slot => !runtime.Characters.Values.Any(other => other.Id != recipient.Id && other.Team == recipient.Team
                && other.Health.Health > 0 && other.Position == slot))
            .Select(slot => (FormationSlot?)slot).FirstOrDefault();
    }
}
