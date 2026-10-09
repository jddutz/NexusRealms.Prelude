using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

/// <summary>An authored operation, including its equipment and learning requirements.</summary>
public sealed record CommandDefinition(AbilityId Id, string Name, float TurnCost,
    ItemId? RequiredItem = null, string? RequiredSlot = null, bool Learned = true,
    bool Passive = false, bool RequiresTarget = true, int FocusCost = 0, Action<CombatSystem, Combatant>? Resolve = null, Func<CommandTarget, bool>? TargetRule = null, string? Icon = null, string? IconRegion = null);
public sealed record CommandTarget(bool Exists, bool Alive, CombatTeam Team, CombatRow Row);
public sealed record CarriedItem(ItemId Id, string Name, string Slot, int InitiativeBonus = 0);

/// <summary>Gameplay-owned command validation and transactional equipment state.</summary>
public sealed class CombatLoadout
{
    public List<CarriedItem> Inventory { get; } = [];
    public List<CommandDefinition> Abilities { get; } = [];
    private Dictionary<string, string> _equipment = [];
    public IReadOnlyDictionary<string, string> Equipment => _equipment;
    public string?[] QuickSlots { get; } = new string?[3];
    public int Focus { get; set; }
    public float EquipmentChangeCost { get; init; } = 0.6f;
    private bool _resolving;
    public CommandDefinition? Find(string? id) => Abilities.Find(a => a.Id.Value == id);
    public bool ValidTarget(CommandDefinition? action, CommandTarget? target) =>
        action is not null && (!action.RequiresTarget || target is { Exists: true, Alive: true }
            && (action.TargetRule?.Invoke(target) ?? target is { Team: CombatTeam.Enemies, Row: CombatRow.Front }));
    public bool Available(CommandDefinition a) => Abilities.Contains(a) && a.Learned && !a.Passive
        && float.IsFinite(a.TurnCost) && a.TurnCost > 0 && a.FocusCost >= 0 && Focus >= a.FocusCost && (a.RequiredItem is null ||
            (a.RequiredSlot is not null && Equipment.GetValueOrDefault(a.RequiredSlot) == a.RequiredItem.Value.Value));
    public bool Assign(int slot, string id)
    {
        var a = Find(id);
        if ((uint)slot >= QuickSlots.Length || a is null || !a.Learned || a.Passive) return false;
        QuickSlots[slot] = id;
        return true;
    }
    public bool CanConfirm(CombatSystem combat, string actorId, CommandDefinition? a, bool validTarget) =>
        !_resolving && !combat.IsResolving && !combat.HasEnded && combat.ActiveCombatant?.Id == actorId
        && combat.ActiveCombatant.PlayerControlled && a is not null && Available(a)
        && (!a.RequiresTarget || validTarget);
    public bool Confirm(CombatSystem combat, string actorId, CommandDefinition? a,
        Func<CommandTarget?> target, Action resolve)
    {
        if (!CanConfirm(combat, actorId, a, ValidTarget(a, target()))) return false;
        _resolving = true;
        try
        {
            combat.SubmitAction(new(a!.TurnCost, (_, _) => { Focus -= a.FocusCost; a.Resolve?.Invoke(combat, combat.ActiveCombatant!); resolve(); }));
            return true;
        }
        finally { _resolving = false; }
    }
    public Dictionary<string, string> BeginEquipment() => new(Equipment);
    public float Cost(IReadOnlyDictionary<string, string> pending) =>
        Equipment.Count == pending.Count && Equipment.All(p => pending.TryGetValue(p.Key, out var v) && v == p.Value)
            ? 0f : EquipmentChangeCost;
    public int InitiativeBonus(IReadOnlyDictionary<string, string> pending) =>
        pending.Values.Sum(id => Inventory.Find(i => i.Id.Value == id)?.InitiativeBonus ?? 0);
    public bool Commit(CombatSystem combat, string actorId, Dictionary<string, string> pending)
    {
        if (_resolving || combat.IsResolving || pending.Any(p => !Inventory.Any(i => i.Id.Value == p.Value && i.Slot == p.Key))
            || pending.Values.Distinct().Count() != pending.Count) return false;
        var cost = Cost(pending);
        if (cost == 0) return true;
        if (combat.HasEnded || combat.ActiveCombatant?.Id != actorId || !combat.ActiveCombatant.PlayerControlled) return false;
        var committed = new Dictionary<string, string>(pending);
        _resolving = true;
        try
        {
            var initiativeChange = InitiativeBonus(committed) - InitiativeBonus(Equipment);
            combat.SubmitAction(new(cost, (_, actor) => { _equipment = committed; actor.Initiative += initiativeChange; }));
            return true;
        }
        finally { _resolving = false; }
    }
}
