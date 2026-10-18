using System.Collections.Immutable;
using NexusRealms.Prelude.Combat;

namespace NexusRealms.Prelude.Actions;

/// <summary>Runtime evaluation of data-only targeting profiles.</summary>
public static class TargetSelection
{
    /// <summary>Dispatches ability targeting to its runtime implementation; legacy actions use the profile.</summary>
    public static ImmutableArray<string> SelectTargets(this ActionData action, CombatActionResolver runtime, string sourceId, string? targetId)
        => (action is AbilityAction abilityAction ? abilityAction.SelectTargets(runtime, sourceId, targetId)
            : action.Targeting.Select(runtime, sourceId, targetId))
            .Where(id => runtime.CanTarget(sourceId, action, id)).ToImmutableArray();

    /// <summary>Selects living recipients with explicit team and formation semantics.</summary>
    public static ImmutableArray<string> Select(this TargetingProfile profile, CombatActionResolver runtime, string sourceId, string? targetId)
    {
        if (!Enum.IsDefined(profile.Shape) || !Enum.IsDefined(profile.Relationship)
            || (profile.Options & ~TargetingOptions.AllTeams) != 0) throw new ArgumentException("Unsupported targeting definition.");
        var source = runtime.Characters[sourceId];
        if (profile.Shape == TargetShape.Self) return [sourceId];
        var requiresTarget = profile.Shape
            is not (
                TargetShape.All
                    or TargetShape.Cone
                    or TargetShape.FrontRow
                    or TargetShape.MiddleRow
                    or TargetShape.BackRow
            );
        if (
            requiresTarget
            && (
                targetId is null
                || !runtime.Characters.TryGetValue(targetId, out var target)
                || target.Health.Health == 0
            )
        )
            return [];
        var anchor = targetId is not null && runtime.Characters.TryGetValue(targetId, out var selected) ? selected.Position : null;
        return runtime.Characters.Values.Where(character => character.Health.Health > 0
            && (profile.Options.HasFlag(TargetingOptions.AllTeams)
                || profile.Relationship == TargetRelationship.Any
                || (character.Team == source.Team) == (profile.Relationship == TargetRelationship.SameTeam))
            && (profile.Shape switch
            {
                TargetShape.Single => character.Id == targetId,
                TargetShape.Melee => character.Id == targetId,
                TargetShape.All => true,
                TargetShape.FrontRow => character.Position is FormationSlot.FrontLeft
                    or FormationSlot.FrontCenter or FormationSlot.FrontRight,
                TargetShape.MiddleRow => character.Position is FormationSlot.MiddleLeft
                    or FormationSlot.MiddleCenterLeft or FormationSlot.MiddleCenterRight
                    or FormationSlot.MiddleRight,
                TargetShape.BackRow => character.Position is FormationSlot.BackLeft
                    or FormationSlot.BackCenter or FormationSlot.BackRight,
                TargetShape.Cone => character.Position is FormationSlot.FrontCenter
                    or FormationSlot.MiddleCenterLeft or FormationSlot.MiddleCenterRight
                    or FormationSlot.BackLeft or FormationSlot.BackCenter or FormationSlot.BackRight,
                TargetShape.Row => anchor is { } row && character.Position is { } position && Coordinates(position).Row == Coordinates(row).Row,
                TargetShape.Column => anchor is { } column && character.Position is { } position && Coordinates(position).Column == Coordinates(column).Column,
                TargetShape.Adjacent => anchor is { } adjacent && character.Position is { } position
                    && Math.Abs(Coordinates(position).Row - Coordinates(adjacent).Row) + Math.Abs(Coordinates(position).Column - Coordinates(adjacent).Column) == 1,
                TargetShape.AdjacentInRow => anchor is { } adjacentInRow && character.Position is { } positionInRow
                    && Coordinates(positionInRow).Row == Coordinates(adjacentInRow).Row
                    && Math.Abs((int)positionInRow - (int)adjacentInRow) == 1,
                _ => false,
            })).OrderBy(character => character.Id, StringComparer.Ordinal).Select(character => character.Id).ToImmutableArray();
    }

    /// <summary>Explicit staggered 3-4-3 coordinates for column and Manhattan-adjacent targeting.</summary>
    internal static (int Row, int Column) Coordinates(FormationSlot slot) => slot switch
    {
        FormationSlot.BackLeft => (0, 0), FormationSlot.BackCenter => (0, 2), FormationSlot.BackRight => (0, 4),
        FormationSlot.MiddleLeft => (1, 0), FormationSlot.MiddleCenterLeft => (1, 1), FormationSlot.MiddleCenterRight => (1, 3), FormationSlot.MiddleRight => (1, 4),
        FormationSlot.FrontLeft => (2, 0), FormationSlot.FrontCenter => (2, 2), FormationSlot.FrontRight => (2, 4),
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}
