using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

/// <summary>An authored operation, including its equipment and learning requirements.</summary>
public sealed record CommandDefinition(AbilityId Id, string Name, float TurnCost,
    ItemId? RequiredItem = null, string? RequiredSlot = null, bool Learned = true,
    bool Passive = false, bool RequiresTarget = true, int FocusCost = 0, Action<CombatSystem, Combatant>? Resolve = null, Func<CommandTarget, bool>? TargetRule = null, string? Icon = null, string? IconRegion = null, StrikeHand? Hand = null);
public enum StrikeHand { Left, Right }
/// <summary>One hit using the held weapon's damage, or one General point when unarmed.</summary>
public sealed record HandStrike(StrikeHand Hand, CarriedItem? Item)
{
    public DamageProfile DamagePoints { get; } = DamageProfile.ForStrike(Item?.Weapon);
    public int Damage => DamagePoints.Total;
}
public sealed record CommandTarget(bool Exists, bool Alive, CombatTeam Team, CombatRow Row,
    bool FrontOccupied = true, bool MiddleOccupied = true);
public sealed record CarriedItem(ItemId Id, string Name, string Slot, string[]? Icons = null, WeaponData? Weapon = null)
{
    public DamageProfile? ThrowDamage { get; init; }
    public int Quantity { get; init; } = 1;
    public int IconIndex { get; init; }
    public string? Icon => ItemIconVariants.Select(Icons, IconIndex);
}

/// <summary>Gameplay-owned command validation and transactional equipment state.</summary>
public sealed class CombatLoadout
{
    /// <summary>Creates the player's initial equipment and shortcuts without spending combat Turn.</summary>
    public static CombatLoadout CreatePlayerLoadout()
    {
        var loadout = new CombatLoadout { Focus = 2 };
        var sword = WeaponCatalog.Create(WeaponType.Sword, "starting-sword", "Sword", "Right hand");
        loadout.Inventory.Add(sword);
        loadout._equipment["Right hand"] = sword.Id.Value;
        loadout.Assign(0, RightHandStrike.Id.Value);
        loadout.Assign(1, LeftHandStrike.Id.Value);
        loadout.Assign(2, Throw.Id.Value);
        loadout.Assign(9, Wait.Id.Value);
        return loadout;
    }

    public static readonly CommandDefinition LeftHandStrike = new("LeftHandStrike", "Left Hand Strike", 1f,
        Icon: "actions.fist.png", Hand: StrikeHand.Left);
    public static readonly CommandDefinition RightHandStrike = new("RightHandStrike", "Right Hand Strike", 1f,
        Icon: "actions.fist.png", Hand: StrikeHand.Right);
    public static readonly CommandDefinition Throw = new("Throw", "Throw", RightHandStrike.TurnCost,
        Icon: "actions.throw.png", TargetRule: target => target.Team == CombatTeam.Enemies && Enum.IsDefined(target.Row));
    // TurnCost is unused for Wait: its actual cost is calculated from the live timeline at confirmation.
    public static readonly CommandDefinition Wait = new("wait", "Wait", 0.01f,
        RequiresTarget: false, Icon: "actions.wait.png");
    public static CommandDefinition? HandAction(string? slot) => slot switch
    {
        "Left hand" => LeftHandStrike, "Right hand" => RightHandStrike, _ => null,
    };
    public CarriedItem? HeldItem(StrikeHand hand) => Inventory.Find(item => item.Id.Value ==
        Equipment.GetValueOrDefault(hand == StrikeHand.Left ? "Left hand" : "Right hand"));
    public string? CommandIcon(CommandDefinition action)
    {
        if (action.Hand is not { } hand) return action.Icon;
        var heldIcon = HeldItem(hand)?.Icon;
        return string.IsNullOrWhiteSpace(heldIcon) ? "actions.fist.png" : heldIcon;
    }
    public static IReadOnlyList<string> EquipmentSlots { get; } =
        ["Head", "Torso", "Feet", "Left hand", "Right hand", "Acc1", "Acc2", "Acc3", "Acc4"];
    public bool CanEquip(CarriedItem item, string slot) => EquipmentSlots.Contains(slot)
        && (item.Slot == slot || item.Weapon is { TwoHanded: true } && slot is "Left hand" or "Right hand"
            || item.Slot == "Accessory" && slot is "Acc1" or "Acc2" or "Acc3" or "Acc4");
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
        "Throw" => Throw,
        "wait" => Wait,
        _ => Abilities.Find(a => a.Id.Value == id),
    };
    public bool ValidTarget(CommandDefinition? action, CommandTarget? target)
    {
        if (action is null) return false;
        if (!action.RequiresTarget) return true;
        if (target is not { Exists: true, Alive: true }) return false;
        if (action.Hand is { } hand)
        {
            if (target.Team != CombatTeam.Enemies || !Enum.IsDefined(target.Row)) return false;
            var range = HeldItem(hand)?.Weapon?.Range ?? WeaponRange.Short;
            return range switch
            {
                WeaponRange.Short => target.Row == (target.FrontOccupied ? CombatRow.Front
                    : target.MiddleOccupied ? CombatRow.Middle : CombatRow.Back),
                WeaponRange.Medium => target.Row is CombatRow.Front or CombatRow.Middle
                    || !target.FrontOccupied && !target.MiddleOccupied,
                WeaponRange.Long => true,
                _ => false,
            };
        }
        return action.TargetRule?.Invoke(target) ?? target is { Team: CombatTeam.Enemies, Row: CombatRow.Front };
    }
    public bool Available(CommandDefinition a) => (Abilities.Contains(a) || ReferenceEquals(a, LeftHandStrike) || ReferenceEquals(a, RightHandStrike) || ReferenceEquals(a, Throw) || ReferenceEquals(a, Wait)) && a.Learned && !a.Passive
        && (a != Throw || ThrowOptions().Count > 0)
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
        && combat.IsAlive(actorId)
        && combat.ActiveCombatant.PlayerControlled && a is not null && Available(a)
        && (a != Wait || combat.WaitCost(actorId) is not null)
        && (!a.RequiresTarget || validTarget);
    public bool Confirm(CombatSystem combat, string actorId, CommandDefinition? a,
        Func<CommandTarget?> target, Action resolve, Action<HandStrike>? strike = null)
    {
        if (!CanConfirm(combat, actorId, a, ValidTarget(a, target()))) return false;
        if (a == Throw) return false; // Throw must commit through its item-selection session.
        if (a!.Hand is not null && strike is null) return false;
        _resolving = true;
        try
        {
            combat.SubmitAction(new(a == Wait ? combat.WaitCost(actorId)!.Value : a.TurnCost, (_, _) =>
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
    public bool HasFreeHand => !Equipment.ContainsKey("Left hand") || !Equipment.ContainsKey("Right hand");
    public IReadOnlyList<ThrowOption> ThrowOptions() => Inventory.Where(item => item.Quantity > 0 && item.ThrowDamage is not null)
        .Select(item => new ThrowOption(item, Equipment.Where(pair => pair.Value == item.Id.Value).Select(pair => pair.Key).ToArray()))
        .Where(option => option.Slots.Length == 0 ? HasFreeHand
            : option.Item.Weapon is { IsMelee: true }
                && option.Slots.All(slot => slot is "Left hand" or "Right hand")).ToArray();
    public List<CarriedItem> ThrownItems { get; } = [];
    public ThrowSelection? BeginThrow(CombatSystem combat, string actorId, Func<CommandTarget?> target) =>
        CanConfirm(combat, actorId, Throw, ValidTarget(Throw, target())) ? new(this, combat, actorId, target, ThrowOptions()) : null;
    internal bool CommitThrow(ThrowSelection selection, ThrowOption option, Action<DamageProfile> damage)
    {
        if (!CanConfirm(selection.Combat, selection.ActorId, Throw, ValidTarget(Throw, selection.Target()))
            || !Inventory.Any(item => ReferenceEquals(item, option.Item))
            || !ThrowOptions().Any(current => ReferenceEquals(current.Item, option.Item) && current.Slots.Order().SequenceEqual(option.Slots.Order()))) return false;
        _resolving = true;
        try
        {
            selection.Combat.SubmitAction(new(Throw.TurnCost, (_, _) =>
            {
                Focus -= Throw.FocusCost;
                foreach (var slot in option.Slots) _equipment.Remove(slot);
                var index = Inventory.IndexOf(option.Item);
                if (option.Item.Quantity == 1) Inventory.RemoveAt(index);
                else Inventory[index] = option.Item with { Quantity = option.Item.Quantity - 1 };
                ThrownItems.Add(option.Item with { Quantity = 1 });
                damage(option.Item.ThrowDamage!);
            }));
            return true;
        }
        finally { _resolving = false; }
    }
    /// <summary>Moves an owned item into a compatible pending slot, swapping equipped items when necessary.</summary>
    public bool TryEquip(Dictionary<string, string> pending, string itemId, string destination)
    {
        var item = Inventory.Find(i => i.Id.Value == itemId);
        if (item is null || !CanEquip(item, destination)) return false;
        if (item.Weapon is { TwoHanded: true } && destination is "Left hand" or "Right hand")
        {
            pending["Left hand"] = itemId;
            pending["Right hand"] = itemId;
            return true;
        }
        if (pending.TryGetValue(destination, out var previous)
            && Inventory.Find(i => i.Id.Value == previous)?.Weapon is { TwoHanded: true })
            foreach (var slot in pending.Where(pair => pair.Value == previous).Select(pair => pair.Key).ToArray()) pending.Remove(slot);
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
            || pending.GroupBy(pair => pair.Value).Any(group =>
                Inventory.Find(item => item.Id.Value == group.Key)?.Weapon is { TwoHanded: true }
                    ? group.Count() != 2 || !group.Any(pair => pair.Key == "Left hand") || !group.Any(pair => pair.Key == "Right hand")
                    : group.Count() != 1)) return false;
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
