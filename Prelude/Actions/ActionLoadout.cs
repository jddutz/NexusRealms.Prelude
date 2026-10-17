using NexusRealms.Prelude.Actions;

namespace NexusRealms.Prelude.Combat;

/// <summary>Runtime ownership and capability operations reuse the existing combat loadout.</summary>
public sealed partial class CombatLoadout
{
    internal CombatLoadout CopyForPreview()
    {
        var copy = new CombatLoadout { Focus = Focus, EquipmentChangeCost = EquipmentChangeCost, BasicCommands = BasicCommands };
        copy.Items.AddRange(Items);
        copy._equipment = new(_equipment);
        copy._inventorySlots = (string?[])_inventorySlots.Clone();
        foreach (var pair in _shots) copy._shots.Add(pair.Key, pair.Value);
        foreach (var pair in _loadedAmmunition) copy._loadedAmmunition.Add(pair.Key, pair.Value);
        copy.LearnedAbilities.UnionWith(LearnedAbilities);
        foreach (var pair in Actions) copy.Actions.Add(pair.Key, pair.Value);
        copy.Commands.AddRange(Commands);
        copy.ThrownItems.AddRange(ThrownItems);
        QuickSlots.CopyTo(copy.QuickSlots, 0);
        copy._pinnedQuickSlots.UnionWith(_pinnedQuickSlots);
        copy.PreparedThrowItemId = PreparedThrowItemId;
        return copy;
    }
    /// <summary>Expands authored character inventory and capabilities into one runtime owner.</summary>
    public static CombatLoadout FromCharacter(CharacterData definition)
    {
        var layout = definition.Equipment.ToDictionary();
        var indexInInventory = 0;
        foreach (var item in definition.Items.Where(item => !definition.Equipment.Values.Contains(item.Id.Value)))
            layout[InventorySlot(indexInInventory++)] = item.Id.Value;
        var loadout = FromState(definition.Items, layout, definition.Focus);
        loadout.BasicCommands = definition.BasicCommands;
        for (var index = 0; index < definition.Actions.Length; index++)
        {
            var action = definition.Actions[index];
            if (action.Id == "attack") action = action with { Id = $"attack-{index}" };
            if (action.ItemId is not null && !action.Classifications.Contains("weapon")) action = action with { Classifications = action.Classifications.Add("weapon") };
            if (!loadout.Actions.TryAdd(action.Id, action)) throw new ArgumentException($"Duplicate action identity '{action.Id}'.");
        }
        return loadout;
    }
    /// <summary>Updates ammunition state during existing AI weapon execution.</summary>
    internal void SetWeaponShots(ItemId item, int shots) => _shots[item.Value] = shots;
    /// <summary>Learned progression identifiers, independent of combat action identities.</summary>
    public HashSet<AbilityId> LearnedAbilities { get; } = [];
    /// <summary>Currently owned action capabilities.</summary>
    public Dictionary<string, ActionData> Actions { get; } = new(StringComparer.Ordinal);

    /// <summary>Learns one progression node and registers its single referenced action.</summary>
    public void Learn(AbilityData ability, AbilityActionRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(ability);
        if (LearnedAbilities.Contains(ability.Id)) return;
        var action = (registry ?? AbilityActionRegistry.Default).Resolve(ability);
        if (string.IsNullOrWhiteSpace(action.Id) || Actions.ContainsKey(action.Id) || Find(action.Id) is not null)
            throw new ArgumentException($"Ability '{ability.Id}' conflicts with action '{action.Id}'.", nameof(ability));
        LearnedAbilities.Add(ability.Id);
        Actions.Add(action.Id, action);
        AddActionCommand(action);
    }

    /// <summary>Derives short melee reach from typed action metadata, owned weapons or hand commands.</summary>
    public bool IsShortMelee(ActionData action)
    {
        if (action.Throw || action.Reload) return false;
        if (action is AbilityAction ability) return ability.GetMeleeRange(this) == WeaponRange.Short;
        if (action.MeleeRange is { } reach) return reach == WeaponRange.Short;
        if (action.ItemId is { } item)
            return Items.Find(owned => owned.Id == item)?.Weapon is { IsMelee: true, Range: WeaponRange.Short };
        if (Find(action.Id)?.Hand is { } hand)
            return HeldItem(hand)?.Weapon is not { } weapon || weapon.IsMelee && weapon.Range == WeaponRange.Short;
        return false;
    }

    /// <summary>Exposes an owned action through the existing HUD without treating its ability as an action.</summary>
    public void AddActionCommand(ActionData action)
    {
        if (Commands.Any(command => command.Id.Value == action.Id)) return;
        Commands.Add(new(action.Id, action.Name ?? action.Id, action.TurnCost,
            RequiresTarget: action.Targeting.Shape is not (TargetShape.Self or TargetShape.All or TargetShape.Cone), TargetRule: target => !target.Elevated || !IsShortMelee(action), Icon: action.Icon?.Value, TargetingFlags: action.Targeting.Flags));
    }

    /// <summary>Checks all progression, ownership, and equipment requirements against current state.</summary>
    public bool MeetsRequirements(ActionData action) => Focus >= action.Requirements.MinimumFocus
        && (action is not AbilityAction ability || ability.CanUse(this))
        && action.Requirements.Abilities.All(LearnedAbilities.Contains)
        && action.Requirements.Items.All(id => Items.Any(item => item.Id == id && item.Quantity > 0))
        && action.Requirements.Equipment.All(requirement => Equipment.TryGetValue(requirement.Slot, out var id)
            && (requirement.Item is null || id == requirement.Item.Value.Value));

    /// <summary>Consumes an owned stack and clears equipment references if it is exhausted.</summary>
    public int ConsumeItem(ItemId id, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var index = Items.FindIndex(item => item.Id == id);
        if (index < 0 || amount == 0) return 0;
        var item = Items[index];
        var used = Math.Min(amount, item.Quantity);
        if (used == item.Quantity) RemoveOwnedItem(id);
        else Items[index] = item with { Quantity = item.Quantity - used };
        return used;
    }

    /// <summary>Removes an owned item atomically with its equipment, ammunition, and inventory references.</summary>
    public bool RemoveOwnedItem(ItemId id)
    {
        var removed = Items.RemoveAll(item => item.Id == id) > 0;
        if (!removed) return false;
        foreach (var slot in _equipment.Where(pair => pair.Value == id.Value).Select(pair => pair.Key).ToArray()) _equipment.Remove(slot);
        for (var index = 0; index < _inventorySlots.Length; index++)
            if (_inventorySlots[index] == id.Value) _inventorySlots[index] = null;
        _shots.Remove(id.Value);
        _loadedAmmunition.Remove(id.Value);
        if (PreparedThrowItemId == id.Value) PreparedThrowItemId = null;
        return true;
    }

    /// <summary>Unequips the item in a slot, preserving ownership and respecting inventory capacity.</summary>
    public bool Unequip(string slot)
    {
        if (!_equipment.TryGetValue(slot, out var id)) return false;
        var next = new Dictionary<string, string>(_equipment);
        foreach (var key in next.Where(pair => pair.Value == id).Select(pair => pair.Key).ToArray()) next.Remove(key);
        var inventory = BuildInventorySlots(next);
        if (inventory is null) return false;
        _equipment = next;
        _inventorySlots = inventory;
        return true;
    }

    /// <summary>Transfers a whole owned stack, preserving identity, quantity, and weapon runtime state.</summary>
    public bool TransferItemTo(CombatLoadout destination, ItemId id, int quantity)
    {
        var item = Items.Find(item => item.Id == id);
        if (ReferenceEquals(this, destination) || item is null || item.Quantity != quantity || !destination.TryAddItem(item)) return false;
        if (_shots.TryGetValue(id.Value, out var shots)) destination._shots[id.Value] = shots;
        if (_loadedAmmunition.TryGetValue(id.Value, out var ammo)) destination._loadedAmmunition[id.Value] = ammo;
        RemoveOwnedItem(id);
        return true;
    }
}
