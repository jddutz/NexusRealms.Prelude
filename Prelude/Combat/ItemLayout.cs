namespace NexusRealms.Prelude.Combat;

/// <summary>Transactional movement between twenty inventory cells and nine equipment cells.</summary>
public sealed partial class CombatLoadout
{
    private bool ValidLayout(IReadOnlyDictionary<string, string> layout)
    {
        if (Items.Any(item => item.Quantity <= 0) || Items.Select(item => item.Id).Distinct().Count() != Items.Count
            || layout.Any(pair => Items.Find(item => item.Id.Value == pair.Value) is not { } item
                || !(IsInventorySlot(pair.Key) || CanEquip(item, pair.Key)))) return false;
        return Items.All(item =>
        {
            var slots = layout.Where(pair => pair.Value == item.Id.Value).Select(pair => pair.Key).ToArray();
            return slots.Length == 1
                ? IsInventorySlot(slots[0]) || item.Weapon is not { TwoHanded: true }
                : item.Weapon is { TwoHanded: true } && slots.Length == 2
                    && slots.Contains("Left hand") && slots.Contains("Right hand");
        });
    }

    private string[] PlacementSlots(CarriedItem item, string destination) =>
        item.Weapon is { TwoHanded: true } && destination is "Left hand" or "Right hand"
            ? ["Left hand", "Right hand"] : [destination];

    private bool CanPlace(CarriedItem item, string destination) => IsInventorySlot(destination) || CanEquip(item, destination);

    /// <summary>Moves a whole item/stack, swapping compatible occupants; failure leaves the layout unchanged.</summary>
    public bool TryMoveItem(Dictionary<string, string> pending, string source, string destination)
    {
        if (!ValidLayout(pending) || !pending.TryGetValue(source, out var itemId)
            || Items.Find(item => item.Id.Value == itemId) is not { } moved || !CanPlace(moved, destination)) return false;
        if (pending.GetValueOrDefault(destination) == itemId) return true;
        var next = new Dictionary<string, string>(pending);
        var targetSlots = PlacementSlots(moved, destination);
        var displaced = targetSlots.Select(slot => next.GetValueOrDefault(slot))
            .Where(id => id is not null && id != itemId).Distinct()
            .Select(id => Items.Find(item => item.Id.Value == id)!).ToArray();
        foreach (var key in next.Where(pair => pair.Value == itemId || displaced.Any(item => item.Id.Value == pair.Value))
            .Select(pair => pair.Key).ToArray()) next.Remove(key);
        foreach (var slot in targetSlots) next[slot] = itemId;
        foreach (var item in displaced)
        {
            var returnSlots = PlacementSlots(item, source);
            if (CanPlace(item, source) && returnSlots.All(slot => !next.ContainsKey(slot)))
            {
                foreach (var slot in returnSlots) next[slot] = item.Id.Value;
                continue;
            }
            // A two-handed move can displace both hands; additional occupants return to free inventory cells.
            if (moved.Weapon is not { TwoHanded: true } && item.Weapon is not { TwoHanded: true }) return false;
            var empty = Enumerable.Range(0, InventoryCapacity).Select(InventorySlot).FirstOrDefault(slot => !next.ContainsKey(slot));
            if (empty is null) return false;
            next[empty] = item.Id.Value;
        }
        if (!ValidLayout(next)) return false;
        pending.Clear();
        foreach (var pair in next) pending.Add(pair.Key, pair.Value);
        return true;
    }

    public IReadOnlyList<string> ChangedItemSlots(IReadOnlyDictionary<string, string> pending)
    {
        var committed = BeginItemLayout();
        return committed.Keys.Union(pending.Keys)
            .Where(slot => committed.GetValueOrDefault(slot) != pending.GetValueOrDefault(slot)).ToArray();
    }

    public float ItemLayoutCost(IReadOnlyDictionary<string, string> pending, IReadOnlyDictionary<string, string>? reloads = null) =>
        ChangedItemSlots(pending).Count * EquipmentChangeCost
        + (reloads?.Keys.Sum(id => Items.Find(item => item.Id.Value == id)?.Weapon?.ReloadTime ?? 0f) ?? 0f);

    public bool CommitItemLayout(CombatSystem combat, string actorId, Dictionary<string, string> pending,
        IReadOnlyDictionary<string, string>? reloads = null)
    {
        if (!ValidLayout(pending)) return false;
        var equipment = pending.Where(pair => EquipmentSlots.Contains(pair.Key)).ToDictionary();
        var inventory = Enumerable.Range(0, InventoryCapacity).Select(index => pending.GetValueOrDefault(InventorySlot(index))).ToArray();
        return CommitCore(combat, actorId, equipment, reloads, inventory, ItemLayoutCost(pending, reloads));
    }
}
