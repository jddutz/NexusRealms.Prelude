using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

/// <summary>An authored operation, including its equipment and learning requirements.</summary>
public sealed record CommandDefinition(ActionId Id, string Name, float TurnCost,
    ItemId? RequiredItem = null, string? RequiredSlot = null, bool Learned = true,
    bool Passive = false, bool RequiresTarget = true, int FocusCost = 0, Action<CombatSystem, Combatant>? Resolve = null, Func<CommandTarget, bool>? TargetRule = null, string? Icon = null, string? IconRegion = null, StrikeHand? Hand = null,
    ImmutableArray<string> Classifications = default, ActionFlags Flags = ActionFlags.None);
public enum StrikeHand { Left, Right }
/// <summary>One hit using the held weapon's damage, or one Basic point when unarmed.</summary>
public sealed record HandStrike(StrikeHand Hand, CarriedItem? Item)
{
    public DamageProfile DamagePoints { get; } = DamageProfile.ForStrike(Item?.Weapon);
    public int Damage => DamagePoints.Total;
    public CarriedItem? Ammunition { get; init; }
}
public sealed record CommandTarget(bool Exists, bool Alive, CombatTeam Team, CombatRow Row,
    bool FrontOccupied = true, bool MiddleOccupied = true, bool Elevated = false);
/// <summary>Gameplay-owned command validation and transactional equipment state.</summary>
public sealed partial class CombatLoadout
{
    /// <summary>Creates the player's initial equipment and shortcuts without spending combat Turn.</summary>
    public static CombatLoadout CreatePlayerLoadout()
    {
        var loadout = new CombatLoadout { Focus = 2 };
        var sword = WeaponCatalog.Create(WeaponType.Sword, "starting-sword", "Sword", "Right hand");
        loadout.Items.Add(sword);
        loadout.Items.Add(WeaponCatalog.Create(WeaponType.Rock, "starting-rocks", "Rock", "Right hand") with { Quantity = 5 });
        loadout._equipment["Right hand"] = sword.Id.Value;
        loadout.Assign(0, RightHandStrike.Id.Value);
        loadout.Assign(1, LeftHandStrike.Id.Value);
        loadout.Assign(9, Wait.Id.Value);
        loadout._pinnedQuickSlots.UnionWith([0, 1, 9]);
        return loadout;
    }

    public static readonly CommandDefinition LeftHandStrike = new("LeftHandStrike", "Left Hand Attack", 1f,
        Icon: "actions.fist.png", Hand: StrikeHand.Left, Flags: ActionFlags.Visual | ActionFlags.Melee);
    public static readonly CommandDefinition RightHandStrike = new("RightHandStrike", "Right Hand Attack", 1f,
        Icon: "actions.fist.png", Hand: StrikeHand.Right, Flags: ActionFlags.Visual | ActionFlags.Melee);
    public static readonly CommandDefinition Throw = new("Throw", "Throw", RightHandStrike.TurnCost,
        Icon: "actions.throw.png", Flags: ActionFlags.Visual, TargetRule: target => target.Team == CombatTeam.Enemies && Enum.IsDefined(target.Row));
    private readonly Dictionary<string, int> _shots = [];
    private readonly Dictionary<string, CarriedItem> _loadedAmmunition = [];
    public int ShotsRemaining(CarriedItem item) => _shots.GetValueOrDefault(item.Id.Value, item.Weapon?.Shots ?? 0);
    public bool HasShootingHands(StrikeHand hand) => HeldItem(hand) is not { Weapon.TwoHanded: true } item
        || HeldItem(hand == StrikeHand.Left ? StrikeHand.Right : StrikeHand.Left)?.Id == item.Id;
    /// <summary>Unmodified command cost, supplied to the scheduler exactly once.</summary>
    public float BaseCommandCost(CommandDefinition action) => action.Hand is { } hand
        ? HeldItem(hand)?.Weapon?.AttackSpeed ?? action.TurnCost : action.TurnCost;
    /// <summary>Live cost for presentation and availability checks.</summary>
    public float CommandCost(CommandDefinition action) => PreviewTurnCost(BaseCommandCost(action));
    /// <summary>Previews a transaction cost without applying modifiers to a free transaction.</summary>
    public float PreviewTurnCost(float baseCost) => baseCost == 0 ? 0 : AdjustTurnCost?.Invoke(baseCost) ?? baseCost;
    /// <summary>Resolver-bound cost preview; copied loadouts are rebound to their own simulation.</summary>
    internal Func<float, float>? AdjustTurnCost { get; set; }

    // TurnCost is unused for Wait: its actual cost is calculated from the live timeline at confirmation.
    public static readonly CommandDefinition Wait = new("wait", "Wait", 0.01f,
        RequiresTarget: false, Icon: "actions.wait.png");
    public static CommandDefinition? HandAction(string? slot) => slot switch
    {
        "Left hand" => LeftHandStrike, "Right hand" => RightHandStrike, _ => null,
    };
    public CarriedItem? HeldItem(StrikeHand hand) => Items.Find(item => item.Id.Value ==
        Equipment.GetValueOrDefault(hand == StrikeHand.Left ? "Left hand" : "Right hand"));
    public string? CommandIcon(CommandDefinition action)
    {
        if (action == Throw) return Items.Find(item => item.Id.Value == PreparedThrowItemId)?.Icon ?? action.Icon;
        if (action.Hand is not { } hand) return action.Icon;
        var heldIcon = HeldItem(hand)?.Icon;
        return string.IsNullOrWhiteSpace(heldIcon) ? "actions.fist.png" : heldIcon;
    }
    public static IReadOnlyList<string> EquipmentSlots { get; } =
        ["Head", "Torso", "Feet", "Left hand", "Right hand", "Acc1", "Acc2", "Acc3", "Acc4"];
    public bool CanEquip(CarriedItem item, string slot) => EquipmentSlots.Contains(slot)
        && (item.Slot == slot || (item.Slot is "Left hand" or "Right hand") && (slot is "Left hand" or "Right hand")
            || item.Slot == "Accessory" && slot is "Acc1" or "Acc2" or "Acc3" or "Acc4");
    /// <summary>Owned item records; each record occupies one inventory or equipment location.</summary>
    public List<CarriedItem> Items { get; } = [];
    public const int InventoryCapacity = 20;
    private string?[] _inventorySlots = new string?[InventoryCapacity];
    public IReadOnlyList<CarriedItem?> Inventory => BuildInventorySlots(Equipment)?
        .Select(id => Items.Find(item => item.Id.Value == id)).ToArray()
        ?? throw new InvalidOperationException("Inventory is full.");
    public static string InventorySlot(int index) => (uint)index < InventoryCapacity
        ? $"Inventory:{index}" : throw new ArgumentOutOfRangeException(nameof(index));
    public static bool IsInventorySlot(string slot) => slot.StartsWith("Inventory:", StringComparison.Ordinal)
        && int.TryParse(slot.AsSpan(10), out var index) && (uint)index < InventoryCapacity
        && slot == InventorySlot(index);
    private string?[]? BuildInventorySlots(IReadOnlyDictionary<string, string> equipment)
    {
        var slots = (string?[])_inventorySlots.Clone();
        var unequipped = Items.Where(item => !equipment.Values.Contains(item.Id.Value)).ToArray();
        for (var i = 0; i < slots.Length; i++)
            if (!unequipped.Any(item => item.Id.Value == slots[i])) slots[i] = null;
        foreach (var item in unequipped)
        {
            if (slots.Contains(item.Id.Value)) continue;
            var empty = Array.IndexOf(slots, null);
            if (empty < 0) return null;
            slots[empty] = item.Id.Value;
        }
        return slots;
    }
    public Dictionary<string, string> BeginItemLayout()
    {
        var inventory = BuildInventorySlots(Equipment) ?? throw new InvalidOperationException("Inventory is full.");
        _inventorySlots = inventory;
        var layout = BeginEquipment();
        for (var i = 0; i < inventory.Length; i++)
            if (inventory[i] is { } id) layout[InventorySlot(i)] = id;
        return layout;
    }
    public bool TryAddItem(CarriedItem item)
    {
        if (item.Quantity <= 0 || Items.Any(owned => owned.Id == item.Id)) return false;
        var inventory = BuildInventorySlots(Equipment);
        if (inventory is null || !inventory.Contains(null)) return false;
        Items.Add(item);
        _inventorySlots = inventory;
        _inventorySlots[Array.IndexOf(_inventorySlots, null)] = item.Id.Value;
        return true;
    }
    /// <summary>Legacy HUD commands, independent of progression abilities.</summary>
    public List<CommandDefinition> Commands { get; } = [];
    /// <summary>Whether universal player hand, Throw, and Wait commands are part of this repertoire.</summary>
    public bool BasicCommands { get; internal set; } = true;
    /// <summary>Compatibility view of HUD commands; progression uses LearnedAbilities.</summary>
    public List<CommandDefinition> Abilities => Commands;
    private Dictionary<string, string> _equipment = [];
    public IReadOnlyDictionary<string, string> Equipment => _equipment;
    public string?[] QuickSlots { get; } = new string?[10];
    private readonly HashSet<int> _pinnedQuickSlots = [];
    public bool IsQuickSlotPinned(int slot) => _pinnedQuickSlots.Contains(slot);
    private int _focus;
    /// <summary>Current Focus shared by HUD, AI, and action resolution.</summary>
    public int Focus
    {
        get => _focus;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_focus == value) return;
            _focus = value;
            FocusChanged?.Invoke();
        }
    }
    /// <summary>Raised when the shared Focus quantity changes.</summary>
    public event Action? FocusChanged;
    /// <summary>Turn cost for each equipment slot whose final item changes.</summary>
    public float EquipmentChangeCost { get; init; } = 0.6f;
    private bool _resolving;
    /// <summary>Live runtime restrictions applied to legacy commands before selection and commitment.</summary>
    public Func<CommandDefinition, bool>? CommandAllowed { get; set; }
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
            var weapon = HeldItem(hand)?.Weapon;
            var range = weapon?.Range ?? WeaponRange.Short;
            if (target.Elevated && (weapon?.IsMelee ?? true) && range == WeaponRange.Short) return false;
            return CanReachRow(range, target);
        }
        return action.TargetRule?.Invoke(target) ?? target is { Team: CombatTeam.Enemies, Row: CombatRow.Front };
    }

    /// <summary>Determines whether the selected formation row is within the supplied melee range.</summary>
    /// <param name="range">The melee weapon's reach.</param>
    /// <param name="target">The target's current row and opposing formation occupancy.</param>
    /// <returns>True when the target row is reachable at that range.</returns>
    internal static bool CanReachRow(WeaponRange range, CommandTarget target) =>
        range switch
        {
            WeaponRange.Short => target.Row
                == (target.FrontOccupied
                    ? CombatRow.Front
                    : target.MiddleOccupied
                        ? CombatRow.Middle
                        : CombatRow.Back),
            WeaponRange.Medium => target.Row is CombatRow.Front or CombatRow.Middle
                || !target.FrontOccupied && !target.MiddleOccupied,
            WeaponRange.Long => true,
            _ => false,
        };
    public bool Available(CommandDefinition a) => (Abilities.Contains(a) || BasicCommands && (ReferenceEquals(a, LeftHandStrike) || ReferenceEquals(a, RightHandStrike) || ReferenceEquals(a, Throw) || ReferenceEquals(a, Wait))) && a.Learned && !a.Passive && (CommandAllowed?.Invoke(a) ?? true)
        && (a != Throw || ThrowOptions().Count > 0)
        && (a.Hand is not { } shootingHand || HasShootingHands(shootingHand))
        && (a.Hand is not { } attackHand || HeldItem(attackHand) is not { Weapon.Shots: > 0 } weapon || ShotsRemaining(weapon) > 0)
        && float.IsFinite(CommandCost(a)) && CommandCost(a) > 0 && a.FocusCost >= 0 && Focus >= a.FocusCost && (a.RequiredItem is null ||
            (a.RequiredSlot is not null && Equipment.GetValueOrDefault(a.RequiredSlot) == a.RequiredItem.Value.Value));
    public bool CanSelectQuickSlot(int slot) => (uint)slot < QuickSlots.Length
        && Find(QuickSlots[slot]) is { } action && Available(action);
    public bool Assign(int slot, string id)
    {
        var a = Find(id);
        if (a == Throw || (uint)slot >= QuickSlots.Length || a is null || !a.Learned || a.Passive) return false;
        if (IsQuickSlotPinned(slot) && QuickSlots[slot] != id) return false;
        if (_pinnedQuickSlots.Any(index => index != slot && QuickSlots[index] == id)) return false;
        for (var index = 0; index < QuickSlots.Length; index++)
            if (index != slot && QuickSlots[index] == id) QuickSlots[index] = null;
        QuickSlots[slot] = id;
        return true;
    }
    public bool CanConfirm(CombatSystem combat, string actorId, CommandDefinition? a, bool validTarget) =>
        !_resolving && !combat.IsResolving && !combat.HasEnded && combat.ActiveCombatant?.Id == actorId
        && combat.IsAlive(actorId)
        && a is not null && Available(a)
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
            Submit(combat, new(a == Wait ? combat.WaitCost(actorId)!.Value : BaseCommandCost(a), (_, _) =>
            {
                Focus -= a.FocusCost;
                if (a.Hand is { } hand)
                {
                    var item = HeldItem(hand);
                    if (item is { Weapon.Shots: > 0 }) _shots[item.Id.Value] = ShotsRemaining(item) - 1;
                    strike!(new(hand, item)
                    {
                        Ammunition = item?.Weapon?.Ammunition is { } type
                            ? _loadedAmmunition.GetValueOrDefault(item.Id.Value)
                                ?? WeaponCatalog.CreateAmmunition(type, $"{item.Id.Value}-initial-ammo")
                            : null,
                    });
                }
                a.Resolve?.Invoke(combat, combat.ActiveCombatant!);
                resolve();
            }, ApplyTurnCostModifiers: a != Wait));
            return true;
        }
        finally { _resolving = false; }
    }
    public Dictionary<string, string> BeginEquipment() => new(Equipment);
    public bool HasFreeHand => !Equipment.ContainsKey("Left hand") || !Equipment.ContainsKey("Right hand");
    public IReadOnlyList<ThrowOption> ThrowOptions(IReadOnlyDictionary<string, string>? equipment = null)
    {
        equipment ??= Equipment;
        var freeHand = !equipment.ContainsKey("Left hand") || !equipment.ContainsKey("Right hand");
        return Items.Where(item => item.Quantity > 0 && item.ThrowDamage is not null)
            .Select(item => new ThrowOption(item, equipment.Where(pair => EquipmentSlots.Contains(pair.Key) && pair.Value == item.Id.Value).Select(pair => pair.Key).ToArray()))
            .Where(option => option.Slots.Length == 0 ? freeHand
                : option.Item.Weapon is { IsMelee: true }
                    && option.Slots.All(slot => slot is "Left hand" or "Right hand")).ToArray();
    }
    public string? PreparedThrowItemId { get; private set; }
    public bool PrepareThrow(string itemId)
    {
        if (!ThrowOptions().Any(option => option.Item.Id.Value == itemId)) return false;
        PreparedThrowItemId = itemId;
        return true;
    }
    public bool CanThrowPreparedItem => ThrowOptions().Any(option => option.Item.Id.Value == PreparedThrowItemId);
    public bool ConfirmPreparedThrow(CombatSystem combat, string actorId, Func<CommandTarget?> target, Action<DamageProfile> damage)
    {
        var selection = BeginThrow(combat, actorId, target);
        var option = selection?.Options.FirstOrDefault(option => option.Item.Id.Value == PreparedThrowItemId);
        if (option is null) return false;
        if (!selection!.Choose(option, damage)) return false;
        if (!Items.Any(item => item.Id == option.Item.Id && item.Quantity > 0)) PreparedThrowItemId = null;
        return true;
    }
    public List<CarriedItem> ThrownItems { get; } = [];
    public ThrowSelection? BeginThrow(CombatSystem combat, string actorId, Func<CommandTarget?> target) =>
        CanConfirm(combat, actorId, Throw, ValidTarget(Throw, target())) ? new(this, combat, actorId, target, ThrowOptions()) : null;
    internal bool CommitThrow(ThrowSelection selection, ThrowOption option, Action<DamageProfile> damage)
    {
        if (!CanConfirm(selection.Combat, selection.ActorId, Throw, ValidTarget(Throw, selection.Target()))
            || !Items.Any(item => ReferenceEquals(item, option.Item))
            || !ThrowOptions().Any(current => ReferenceEquals(current.Item, option.Item) && current.Slots.Order().SequenceEqual(option.Slots.Order()))) return false;
        _resolving = true;
        try
        {
            Submit(selection.Combat, new(Throw.TurnCost, (_, _) =>
            {
                Focus -= Throw.FocusCost;
                if (option.Item.Quantity == 1)
                    foreach (var slot in option.Slots) _equipment.Remove(slot);
                var index = Items.IndexOf(option.Item);
                if (option.Item.Quantity == 1) Items.RemoveAt(index);
                else Items[index] = option.Item with { Quantity = option.Item.Quantity - 1 };
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
        var item = Items.Find(i => i.Id.Value == itemId);
        if (item is null || !CanEquip(item, destination)) return false;
        if (item.Weapon is { TwoHanded: true } && destination is "Left hand" or "Right hand")
        {
            pending["Left hand"] = itemId;
            pending["Right hand"] = itemId;
            return true;
        }
        if (pending.TryGetValue(destination, out var previous)
            && Items.Find(i => i.Id.Value == previous)?.Weapon is { TwoHanded: true })
            foreach (var slot in pending.Where(pair => pair.Value == previous).Select(pair => pair.Key).ToArray()) pending.Remove(slot);
        var source = pending.FirstOrDefault(pair => pair.Value == itemId).Key;
        if (source == destination) return true;
        var displacedId = pending.GetValueOrDefault(destination);
        if (source is not null && displacedId is not null)
        {
            var displaced = Items.Find(i => i.Id.Value == displacedId);
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
    public float Cost(IReadOnlyDictionary<string, string> pending, IReadOnlyDictionary<string, string>? reloads = null) =>
        ChangedEquipmentSlots(pending).Count * EquipmentChangeCost
        + (reloads?.Keys.Sum(id => Items.Find(item => item.Id.Value == id)?.Weapon?.ReloadTime ?? 0f) ?? 0f);
    public bool CanReload(IReadOnlyDictionary<string, string> pending, string weaponId, string ammunitionId) =>
        pending.Any(pair => EquipmentSlots.Contains(pair.Key) && pair.Value == weaponId)
        && !pending.Any(pair => EquipmentSlots.Contains(pair.Key) && pair.Value == ammunitionId)
        && Items.Find(item => item.Id.Value == weaponId) is { Weapon: { Shots: > 0, Ammunition: { } type } } weapon
        && ShotsRemaining(weapon) < weapon.Weapon!.Shots
        && Items.Find(item => item.Id.Value == ammunitionId) is { Quantity: > 0 } ammo && ammo.Ammunition == type;

    public bool Commit(CombatSystem combat, string actorId, Dictionary<string, string> pending, IReadOnlyDictionary<string, string>? reloads = null) =>
        CommitCore(combat, actorId, pending, reloads, BuildInventorySlots(pending), Cost(pending, reloads));
    private bool CommitCore(CombatSystem combat, string actorId, Dictionary<string, string> pending,
        IReadOnlyDictionary<string, string>? reloads, string?[]? inventorySlots, float cost, bool validateOnly = false)
    {
        if (inventorySlots is null || _resolving || combat.IsResolving || pending.Any(p => !Items.Any(i => i.Id.Value == p.Value && CanEquip(i, p.Key)))
            || pending.GroupBy(pair => pair.Value).Any(group =>
                Items.Find(item => item.Id.Value == group.Key)?.Weapon is { TwoHanded: true }
                    ? group.Count() != 2 || !group.Any(pair => pair.Key == "Left hand") || !group.Any(pair => pair.Key == "Right hand")
                    : group.Count() != 1)) return false;
        var queuedReloads = reloads is null ? new Dictionary<string, string>() : new Dictionary<string, string>(reloads);
        if (queuedReloads.Any(pair => !CanReload(pending, pair.Key, pair.Value))
            || queuedReloads.GroupBy(pair => pair.Value).Any(group =>
                Items.Find(item => item.Id.Value == group.Key)!.Quantity < group.Sum(pair =>
                    Items.Find(item => item.Id.Value == pair.Key)!.Weapon!.Shots - ShotsRemaining(Items.Find(item => item.Id.Value == pair.Key)!)))) return false;
        if (cost == 0f && queuedReloads.Count == 0) return true;
        if (!float.IsFinite(cost) || cost <= 0f) return false;
        if (!LayoutAllowed(queuedReloads, cost)) return false;
        if (combat.HasEnded || combat.ActiveCombatant?.Id != actorId || !combat.IsAlive(actorId)) return false;
        if (validateOnly) return true;
        var committed = new Dictionary<string, string>(pending);
        _resolving = true;
        try
        {
            Submit(combat, new(cost, (_, _) =>
            {
                _equipment = committed;
                _inventorySlots = (string?[])inventorySlots.Clone();
                foreach (var (weaponId, ammoId) in queuedReloads)
                {
                    var weapon = Items.Find(item => item.Id.Value == weaponId)!;
                    var index = Items.FindIndex(item => item.Id.Value == ammoId);
                    var ammo = Items[index];
                    var used = weapon.Weapon!.Shots - ShotsRemaining(weapon);
                    if (ammo.Quantity == used) Items.RemoveAt(index);
                    else Items[index] = ammo with { Quantity = ammo.Quantity - used };
                    _shots[weaponId] = weapon.Weapon.Shots;
                    _loadedAmmunition[weaponId] = ammo with { Quantity = 1 };
                }
                combat.Resolver?.RecordEmptyAction(actorId, queuedReloads.Count > 0 ? "reload" : "equipment", cost);
            }));
            return true;
        }
        finally { _resolving = false; }
    }

    private static void Submit(CombatSystem combat, CombatAction operation)
    {
        if (combat.ActiveCombatant!.PlayerControlled) combat.SubmitAction(operation);
        else combat.SubmitAutomaticAction(operation);
    }
    private bool LayoutAllowed(IReadOnlyDictionary<string, string> reloads, float cost) => CommandAllowed?.Invoke(
        new(reloads.Count > 0 ? "reload" : "equipment", "Equipment", cost, RequiresTarget: false,
            Classifications: reloads.Count > 0 ? ["weapon", "reload"] : ["equipment"])) ?? true;
}
