using NexusRealms.Prelude.Actions;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

public enum CandidateKind { Capability, LegacyWeapon, Command, Throw, ItemLayout, Escape }
public sealed record ActionCandidate(ActionData Action, string? TargetId)
{
    public CandidateKind Kind { get; init; }
    public string? ItemId { get; init; }
    public ImmutableDictionary<string, string> Layout { get; init; } = ImmutableDictionary<string, string>.Empty;
    public ImmutableDictionary<string, string> Reloads { get; init; } = ImmutableDictionary<string, string>.Empty;
    public string ChoiceKey => string.Concat(Layout.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => Part(pair.Key) + Part(pair.Value)))
        + "/" + string.Concat(Reloads.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => Part(pair.Key) + Part(pair.Value)));
    public string Key => Part(Action.Id) + Part(TargetId ?? "") + Part(ItemId ?? "") + Part(Kind.ToString()) + ChoiceKey;
    private static string Part(string value) => value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
    public static IOrderedEnumerable<ActionCandidate> Order(IEnumerable<ActionCandidate> candidates) => candidates
        .OrderBy(candidate => candidate.Action.Id, StringComparer.Ordinal).ThenBy(candidate => candidate.TargetId, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.ItemId, StringComparer.Ordinal).ThenBy(candidate => candidate.Kind).ThenBy(candidate => candidate.ChoiceKey, StringComparer.Ordinal);
}
public sealed record RejectedCandidate(string Key, string Reason);

/// <summary>Complete choices validated by the same owners that commit them.</summary>
public static class CandidateGeneration
{
    public static IReadOnlyList<ActionCandidate> Enumerate(CombatActionResolver resolver, string actorId, List<RejectedCandidate>? rejected = null, bool includeLayouts = true)
    {
        var actor = resolver.Characters[actorId];
        var loadout = actor.Loadout;
        var candidates = new List<ActionCandidate>();
        var targets = resolver.Characters.Keys.Order(StringComparer.Ordinal).ToArray();
        void Add(ActionCandidate candidate)
        {
            if (resolver.CanCommit(actorId, candidate)) candidates.Add(candidate);
            else rejected?.Add(new(candidate.Key, resolver.ExplainUnavailable(actorId, candidate)));
        }
        foreach (var definition in loadout.Actions.Values.OrderBy(action => action.Id, StringComparer.Ordinal))
        {
            var legacy = definition.Effects is null && definition.ItemId is not null;
            var action = legacy ? new EnemyActionState(loadout).Prepare(definition) : definition;
            if (action is null) { rejected?.Add(new(definition.Id, "Owned weapon or required ammunition unavailable.")); continue; }
            // Reload is an item-layout transaction below, with every ammunition choice explicit.
            if (legacy && action.Reload) continue;
            foreach (var target in action.Reload || action.Targeting.Shape is TargetShape.Self or TargetShape.All ? new string?[] { null } : targets)
                Add(new(action, target) { Kind = legacy ? CandidateKind.LegacyWeapon : CandidateKind.Capability, ItemId = action.ItemId?.Value });
        }
        foreach (var command in loadout.Commands.Where(command => !loadout.Actions.ContainsKey(command.Id.Value))
            .Concat([CombatLoadout.LeftHandStrike, CombatLoadout.RightHandStrike, CombatLoadout.Wait]).Distinct().OrderBy(command => command.Id.Value, StringComparer.Ordinal))
        {
            if (command == CombatLoadout.Throw) continue;
            if (command.Resolve is not null) throw new InvalidOperationException($"AI command '{command.Id}' has an opaque callback; author typed effects instead.");
            var damage = command.Hand is { } hand ? DamageProfile.ForStrike(loadout.HeldItem(hand)?.Weapon) : new DamageProfile();
            var cost = command == CombatLoadout.Wait ? resolver.Combat.WaitCost(actorId) : loadout.CommandCost(command);
            if (cost is null) continue;
            var action = new ActionData { Id = command.Id.Value, Damage = damage, TurnCost = cost.Value,
                Classifications = command.Hand is not null ? ["weapon"] : command.Classifications.IsDefault ? [] : command.Classifications,
                Costs = [new(new ResourceSubject(ResourceKind.Focus), command.FocusCost)] };
            foreach (var target in command.RequiresTarget ? targets : new string?[] { null })
                Add(new(action, target) { Kind = CandidateKind.Command, ItemId = command.Hand is { } h ? loadout.HeldItem(h)?.Id.Value : null });
        }
        foreach (var option in loadout.ThrowOptions().OrderBy(option => option.Item.Id.Value, StringComparer.Ordinal))
            foreach (var target in targets)
                Add(new(new ActionData { Id = "Throw", Damage = option.Item.ThrowDamage!, TurnCost = CombatLoadout.Throw.TurnCost,
                    Classifications = ["weapon"], Costs = [new(new ResourceSubject(ResourceKind.Focus), CombatLoadout.Throw.FocusCost)] }, target)
                    { Kind = CandidateKind.Throw, ItemId = option.Item.Id.Value });
        if (actor.IsPlayer && resolver.AllowEscape && !resolver.Combat.IsEscaping)
            Add(new(new ActionData { Id = "escape", TurnCost = 1f, Damage = new DamageProfile() }, null) { Kind = CandidateKind.Escape });
        if (!includeLayouts) return ActionCandidate.Order(candidates.DistinctBy(candidate => candidate.Key)).ToArray();
        // Reading the layout on a copy avoids normalizing live inventory during evaluation.
        var layout = loadout.CopyForPreview().BeginItemLayout();
        foreach (var source in layout.Keys.Order(StringComparer.Ordinal))
            foreach (var destination in CombatLoadout.EquipmentSlots.Concat(Enumerable.Range(0, CombatLoadout.InventoryCapacity).Select(CombatLoadout.InventorySlot)))
            {
                if (CombatLoadout.IsInventorySlot(source) && CombatLoadout.IsInventorySlot(destination)) continue;
                var pending = new Dictionary<string, string>(layout);
                if (loadout.TryMoveItem(pending, source, destination))
                {
                    var cost = loadout.CopyForPreview().ItemLayoutCost(pending);
                    if (cost > 0) Add(new(new ActionData { Id = "equipment", TurnCost = cost, Damage = new DamageProfile() }, null)
                        { Kind = CandidateKind.ItemLayout, Layout = pending.ToImmutableDictionary() });
                }
            }
        foreach (var weapon in loadout.Items.OrderBy(item => item.Id.Value, StringComparer.Ordinal))
            foreach (var ammo in loadout.Items.Where(ammo => loadout.CanReload(layout, weapon.Id.Value, ammo.Id.Value)).OrderBy(item => item.Id.Value, StringComparer.Ordinal))
                Add(new(new ActionData { Id = "reload", TurnCost = weapon.Weapon!.ReloadTime, Damage = new DamageProfile(), Reload = true,
                    ReloadAnimation = loadout.Actions.Values.Where(action => action.ItemId == weapon.Id).OrderBy(action => action.Id, StringComparer.Ordinal).FirstOrDefault()?.ReloadAnimation ?? "Reload",
                    Animation = loadout.Actions.Values.Where(action => action.ItemId == weapon.Id).OrderBy(action => action.Id, StringComparer.Ordinal).FirstOrDefault()?.ReloadAnimation ?? "Reload" }, null)
                    { Kind = CandidateKind.ItemLayout, ItemId = weapon.Id.Value, Layout = layout.ToImmutableDictionary(), Reloads = ImmutableDictionary<string, string>.Empty.Add(weapon.Id.Value, ammo.Id.Value) });
        return ActionCandidate.Order(candidates.DistinctBy(candidate => candidate.Key)).ToArray();
    }
    internal static CommandTarget? Target(CombatActionResolver resolver, string actorId, string? targetId)
    {
        if (targetId is null || !resolver.Characters.TryGetValue(targetId, out var target)) return null;
        var actor = resolver.Characters[actorId];
        var occupied = resolver.Characters.Values.Where(character => character.Team != actor.Team && resolver.Combat.IsAlive(character.Id))
            .Select(character => character.Position is { } slot ? Row(slot) : CombatRow.Front).ToHashSet();
        return new(resolver.Combat.Contains(targetId), resolver.Combat.IsAlive(targetId),
            target.Team != actor.Team ? CombatTeam.Enemies : CombatTeam.PlayerAndAllies,
            target.Position is { } position ? Row(position) : CombatRow.Front, occupied.Contains(CombatRow.Front), occupied.Contains(CombatRow.Middle));
    }
    private static CombatRow Row(FormationSlot slot) => slot >= FormationSlot.FrontLeft ? CombatRow.Front : slot >= FormationSlot.MiddleLeft ? CombatRow.Middle : CombatRow.Back;
}
