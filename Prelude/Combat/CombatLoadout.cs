using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

/// <summary>An authored operation, including its equipment and learning requirements.</summary>
public sealed record CommandDefinition(AbilityId Id, string Name, float TurnCost,
    ItemId? RequiredItem = null, string? RequiredSlot = null, bool Learned = true,
    bool Passive = false, bool RequiresTarget = true, int FocusCost = 0, Action<CombatSystem, Combatant>? Resolve = null, Func<CommandTarget, bool>? TargetRule = null, string? Icon = null, string? IconRegion = null, StrikeHand? Hand = null);
public enum StrikeHand { Left, Right }
/// <summary>One hit: an untyped base point plus the held weapon's typed bonus points.</summary>
public sealed record HandStrike(StrikeHand Hand, CarriedItem? Item)
{
    public DamageProfile DamagePoints { get; } = DamageProfile.ForStrike(Item?.Weapon);
    public int Damage => DamagePoints.Total;
}
public sealed record CommandTarget(bool Exists, bool Alive, CombatTeam Team, CombatRow Row);
public sealed record CarriedItem(ItemId Id, string Name, string Slot, string? Icon = null, WeaponData? Weapon = null);

/// <summary>Gameplay-owned command validation and transactional equipment state.</summary>
public sealed class CombatLoadout
{
    public static readonly CommandDefinition LeftHandStrike = new("LeftHandStrike", "Left Hand Strike", 1f,
        Icon: "stats.fist.png", Hand: StrikeHand.Left);
    public static readonly CommandDefinition RightHandStrike = new("RightHandStrike", "Right Hand Strike", 1f,
        Icon: "stats.fist.png", Hand: StrikeHand.Right);
    public static CommandDefinition? HandAction(string? slot) => slot switch
    {
        "Left hand" => LeftHandStrike, "Right hand" => RightHandStrike, _ => null,
    };
    public CarriedItem? HeldItem(StrikeHand hand) => Inventory.Find(item => item.Id.Value ==
        Equipment.GetValueOrDefault(hand == StrikeHand.Left ? "Left hand" : "Right hand"));
    public string? CommandIcon(CommandDefinition action) => action.Hand is { } hand
        ? HeldItem(hand)?.Icon ?? "stats.fist.png" : action.Icon;
    public static IReadOnlyList<string> EquipmentSlots { get; } =
        ["Head", "Torso", "Feet", "Left hand", "Right hand", "Acc1", "Acc2", "Acc3", "Acc4"];
    public bool CanEquip(CarriedItem item, string slot) => EquipmentSlots.Contains(slot)
        && (item.Slot == slot || item.Slot == "Accessory" && slot is "Acc1" or "Acc2" or "Acc3" or "Acc4");
    public List<CarriedItem> Inventory { get; } = [];
    public List<CommandDefinition> Abilities { get; } = [];
    private Dictionary<string, string> _equipment = [];
    public IReadOnlyDictionary<string, string> Equipment => _equipment;
    public string?[] QuickSlots { get; } = new string?[10];
    public int Focus { get; set; }
    /// <summary>Turn cost for each equipment slot whose final item changes.</summary>
    public float EquipmentChangeCost { get; init; } = 0.6f;
    private bool _resolving;
    public CommandDefinition? Find(string? id) => id switch
    {
        "LeftHandStrike" => LeftHandStrike, "RightHandStrike" => RightHandStrike,
        _ => Abilities.Find(a => a.Id.Value == id),
    };
    public bool ValidTarget(CommandDefinition? action, CommandTarget? target) =>
        action is not null && (!action.RequiresTarget || target is { Exists: true, Alive: true }
            && (action.TargetRule?.Invoke(target) ?? target is { Team: CombatTeam.Enemies, Row: CombatRow.Front }));
    public bool Available(CommandDefinition a) => (Abilities.Contains(a) || ReferenceEquals(a, LeftHandStrike) || ReferenceEquals(a, RightHandStrike)) && a.Learned && !a.Passive
        && float.IsFinite(a.TurnCost) && a.TurnCost > 0 && a.FocusCost >= 0 && Focus >= a.FocusCost && (a.RequiredItem is null ||
            (a.RequiredSlot is not null && Equipment.GetValueOrDefault(a.RequiredSlot) == a.RequiredItem.Value.Value));
    public bool CanSelectQuickSlot(int slot) => (uint)slot < QuickSlots.Length
        && Find(QuickSlots[slot]) is { } action && Available(action);
    public bool Assign(int slot, string id)
    {
        var a = Find(id);
        if ((uint)slot >= QuickSlots.Length || a is null || !a.Learned || a.Passive) return false;
        for (var index = 0; index < QuickSlots.Length; index++)
            if (index != slot && QuickSlots[index] == id) QuickSlots[index] = null;
        QuickSlots[slot] = id;
        return true;
    }
    public bool CanConfirm(CombatSystem combat, string actorId, CommandDefinition? a, bool validTarget) =>
        !_resolving && !combat.IsResolving && !combat.HasEnded && combat.ActiveCombatant?.Id == actorId
        && combat.ActiveCombatant.PlayerControlled && a is not null && Available(a)
        && (!a.RequiresTarget || validTarget);
    public bool Confirm(CombatSystem combat, string actorId, CommandDefinition? a,
        Func<CommandTarget?> target, Action resolve, Action<HandStrike>? strike = null)
    {
        if (!CanConfirm(combat, actorId, a, ValidTarget(a, target()))) return false;
        if (a!.Hand is not null && strike is null) return false;
        _resolving = true;
        try
        {
            combat.SubmitAction(new(a.TurnCost, (_, _) =>
            {
                Focus -= a.FocusCost;
                if (a.Hand is { } hand) strike!(new(hand, HeldItem(hand)));
                a.Resolve?.Invoke(combat, combat.ActiveCombatant!);
                resolve();
            }));
            return true;
        }
        finally { _resolving = false; }
    }
    public Dictionary<string, string> BeginEquipment() => new(Equipment);
    /// <summary>Moves an owned item into a compatible pending slot, swapping equipped items when necessary.</summary>
    public bool TryEquip(Dictionary<string, string> pending, string itemId, string destination)
    {
        var item = Inventory.Find(i => i.Id.Value == itemId);
        if (item is null || !CanEquip(item, destination)) return false;
        var source = pending.FirstOrDefault(pair => pair.Value == itemId).Key;
        if (source == destination) return true;
        var displacedId = pending.GetValueOrDefault(destination);
        if (source is not null && displacedId is not null)
        {
            var displaced = Inventory.Find(i => i.Id.Value == displacedId);
            if (displaced is null || !CanEquip(displaced, source)) return false;
        }
        if (source is not null)
        {
            if (displacedId is not null) pending[source] = displacedId;
            else pending.Remove(source);
        }
        pending[destination] = itemId;
        return true;
    }
    public IReadOnlyList<string> ChangedEquipmentSlots(IReadOnlyDictionary<string, string> pending) =>
        Equipment.Keys.Union(pending.Keys)
            .Where(slot => Equipment.GetValueOrDefault(slot) != pending.GetValueOrDefault(slot)).ToArray();
    public float Cost(IReadOnlyDictionary<string, string> pending) =>
        ChangedEquipmentSlots(pending).Count * EquipmentChangeCost;
    public bool Commit(CombatSystem combat, string actorId, Dictionary<string, string> pending)
    {
        if (_resolving || combat.IsResolving || pending.Any(p => !Inventory.Any(i => i.Id.Value == p.Value && CanEquip(i, p.Key)))
            || pending.Values.Distinct().Count() != pending.Count) return false;
        var cost = Cost(pending);
        if (ChangedEquipmentSlots(pending).Count == 0) return true;
        if (!float.IsFinite(cost) || cost <= 0f) return false;
        if (combat.HasEnded || combat.ActiveCombatant?.Id != actorId || !combat.ActiveCombatant.PlayerControlled) return false;
        var committed = new Dictionary<string, string>(pending);
        _resolving = true;
        try
        {
            combat.SubmitAction(new(cost, (_, _) => _equipment = committed));
            return true;
        }
        finally { _resolving = false; }
    }
}
