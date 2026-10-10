using NexusRealms.Prelude.DataModel;
using NexusRealms.Prelude.Combat;

static void Check(bool condition) { if (!condition) throw new Exception("Check failed."); }
var timeline = new CombatTimeline();
var later = timeline.Schedule(4f, "later");
var first = timeline.Schedule(2f, "effect");
var second = timeline.Schedule(2f, "second effect");
timeline.Schedule(2f, "actor", TimelinePriority.Combatant);
Check(timeline.Advance()!.Id == first);
Check(timeline.Advance()!.Id == second);
Check(timeline.CurrentTurn == 2f);
timeline.Adjust(later, -10f);
Check(timeline.Preview(2)[0].Id == later);
Check(timeline.Cancel(later));
Check(timeline.Advance()!.Label == "actor");
Check(timeline.Advance() is null);

var combat = new CombatSystem();
var player = new Combatant("player", true, 2.4f);
combat.Add(player);
combat.Add(new Combatant("enemy", false, 3.4f));
var enemyActions = 0;
combat.DecideAction = (_, _) => new(1f, (_, _) => enemyActions++);
var periodic = new List<float>();
void Recur(CombatSystem system)
{
    periodic.Add(system.Timeline.CurrentTurn);
    if (periodic.Count < 2) system.ScheduleEvent(system.Timeline.CurrentTurn + 1f, "focus", Recur);
}
combat.ScheduleEvent(2.7f, "focus", Recur);
var expired = false;
combat.ScheduleEvent(3.2f, "expiration", _ => expired = true);
var canceled = combat.ScheduleEvent(2.5f, "removed status", _ => throw new Exception());
combat.CancelEvent(canceled);
combat.Process();
Check(combat.Timeline.CurrentTurn == 2.4f && combat.ActiveCombatant == player);
combat.Process();
Check(combat.Timeline.CurrentTurn == 2.4f);
combat.SubmitAction(new(0.4f, (_, _) => { }));
Check(enemyActions == 0 && periodic.Count == 1);
Check(combat.ActiveCombatant == player && player.Turn == 2.4f + 0.4f);
combat.AdjustTurn("enemy", -100f);
Check(combat.Timeline.Preview(1)[0].Turn == combat.Timeline.CurrentTurn);
combat.SubmitAction(new(1f, (_, _) => { }));
Check(enemyActions == 1 && expired && periodic.Count == 2);
Check(periodic[1] == periodic[0] + 1f);
try { combat.SubmitAction(new(0f, (_, _) => { })); throw new Exception("Accepted zero cost"); }
catch (ArgumentOutOfRangeException) { }
combat.End();
combat.Process();
Check(combat.Timeline.TurnOrder.Count == 0 && combat.ActiveCombatant is null);
var bounded = new CombatSystem();
var executions = 0;
void SameTime(CombatSystem system) { executions++; system.ScheduleEvent(0f, "repeat", SameTime); }
bounded.ScheduleEvent(0f, "repeat", SameTime);
bounded.Process(10);
Check(executions == 10);
var visibility = new CombatSystem();
var hiddenExecuted = false;
var hidden = visibility.ScheduleEvent(0.25f, "hidden", _ => hiddenExecuted = true,
    isVisible: false, presentationKey: "poison");
visibility.ScheduleEvent(1f, "visible", _ => { });
Check(visibility.Timeline.Preview(1, visibleOnly: true)[0].Label == "visible");
Check(visibility.Timeline.Preview(1)[0].PresentationKey == "poison");
visibility.Timeline.SetVisibility(hidden, true);
Check(visibility.Timeline.Preview(1, visibleOnly: true)[0].Id == hidden);
visibility.Timeline.SetVisibility(hidden, false);
visibility.Process();
Check(hiddenExecuted && visibility.Timeline.CurrentTurn == 1f);
var ties = new CombatTimeline();
ties.Schedule(1f, "enemy back", TimelinePriority.Combatant, "eb", initiative: 5,
    team: CombatTeam.Enemies, row: CombatRow.Back);
ties.Schedule(1f, "ally back", TimelinePriority.Combatant, "ab", initiative: 5,
    team: CombatTeam.PlayerAndAllies, row: CombatRow.Back);
ties.Schedule(1f, "enemy front", TimelinePriority.Combatant, "ef", initiative: 5,
    team: CombatTeam.Enemies, row: CombatRow.Front);
ties.Schedule(1f, "fast enemy", TimelinePriority.Combatant, "fast", initiative: 10,
    team: CombatTeam.Enemies);
ties.Schedule(1f, "ally front high rank", TimelinePriority.Combatant, "afh", initiative: 5,
    randomRank: 20);
ties.Schedule(1f, "ally front low rank", TimelinePriority.Combatant, "afl", initiative: 5,
    randomRank: 10);
ties.Schedule(1f, "effect");
Check(ties.TurnOrder.Select(x => x.Label).SequenceEqual(new[]
{
    "effect", "fast enemy", "ally front low rank", "ally front high rank",
    "ally back", "enemy front", "enemy back"
}));
var seededA = new CombatSystem(42);
var seededB = new CombatSystem(42);
foreach (var system in new[] { seededA, seededB })
    for (var i = 0; i < 10; i++) system.Add(new Combatant($"actor-{i}", true));
Check(seededA.Timeline.TurnOrder.Select(x => x.CombatantId)
    .SequenceEqual(seededB.Timeline.TurnOrder.Select(x => x.CombatantId)));
var originalRank = seededA.Timeline.TurnOrder[0].RandomRank;
var originalActor = seededA.Timeline.TurnOrder[0].CombatantId!;
seededA.Process();
seededA.SubmitAction(new(0.5f, (_, _) => { }));
Check(seededA.Timeline.TurnOrder.Single(x => x.CombatantId == originalActor).RandomRank == originalRank);
Console.WriteLine("Combat timeline checks passed.");




// Commands and equipment transactions share the timeline and never execute on assignment.
var commandsCombat = new CombatSystem();
commandsCombat.Add(new Combatant("player", true, initiative: 3));
commandsCombat.Process();
var loadout = new CombatLoadout { Focus = 2 };
var commandExecutions = 0;
var slash = new CommandDefinition("slash", "Slash", 0.6f, "sword", "Right hand", FocusCost: 1,
    Resolve: (_, _) => commandExecutions++);
loadout.Inventory.Add(new("sword", "Sword", "Right hand"));
loadout.Abilities.Add(slash);
loadout.Abilities.Add(new("locked", "Locked", 1f, Learned: false));
loadout.Abilities.Add(new("passive", "Passive", 1f, Passive: true));
Check(loadout.Assign(0, "slash") && commandExecutions == 0);
Check(!loadout.Assign(0, "locked") && !loadout.Assign(0, "passive"));
Check(!loadout.Available(slash));
var pendingLoadout = loadout.BeginEquipment();
pendingLoadout["Right hand"] = "sword";
Check(loadout.Equipment.Count == 0 && loadout.Cost(pendingLoadout) == 0.6f);
pendingLoadout.Clear();
Check(loadout.Cost(pendingLoadout) == 0f);
pendingLoadout["Right hand"] = "missing";
var beforeCommit = commandsCombat.Timeline.CurrentTurn;
Check(!loadout.Commit(commandsCombat, "player", pendingLoadout));
Check(loadout.Equipment.Count == 0 && commandsCombat.Timeline.CurrentTurn == beforeCommit);
pendingLoadout["Right hand"] = "sword";
Check(loadout.Commit(commandsCombat, "player", pendingLoadout));
Check(loadout.Equipment["Right hand"] == "sword" && loadout.Available(slash));
Check(commandsCombat.ActiveCombatant!.Initiative == 3);
Check(commandsCombat.Timeline.CurrentTurn == beforeCommit + 0.6f && commandExecutions == 0);
Check(!loadout.Confirm(commandsCombat, "player", slash, () => null, () => { }));
Check(loadout.Focus == 2 && commandExecutions == 0);
Check(loadout.Confirm(commandsCombat, "player", slash, () => new(true, true, CombatTeam.Enemies, CombatRow.Front), () =>
{
    Check(!loadout.Confirm(commandsCombat, "player", slash, () => new(true, true, CombatTeam.Enemies, CombatRow.Front), () => { }));
}));
Check(commandExecutions == 1 && loadout.Focus == 1);
Check(loadout.Confirm(commandsCombat, "player", slash, () => new(true, true, CombatTeam.Enemies, CombatRow.Front), () => { }));
Check(commandExecutions == 2 && loadout.Focus == 0);
Check(!loadout.Confirm(commandsCombat, "player", slash, () => new(true, true, CombatTeam.Enemies, CombatRow.Front), () => { }));
Check(loadout.QuickSlots[0] == "slash");
var canceledEquipment = loadout.BeginEquipment();
canceledEquipment.Clear();
Check(loadout.Equipment["Right hand"] == "sword");
Check(loadout.Commit(commandsCombat, "player", canceledEquipment));
Check(!loadout.Available(slash) && loadout.QuickSlots[0] == "slash");
var wait = new CommandDefinition("wait", "Wait", 0.6f, RequiresTarget: false);
loadout.Abilities.Add(wait);
Check(loadout.Confirm(commandsCombat, "player", wait, () => null, () => { }));
Console.WriteLine("Command selection and equipment transaction checks passed.");

Check(commandsCombat.ActiveCombatant!.Initiative == 3);
Check(!loadout.ValidTarget(slash, new(true, true, CombatTeam.Enemies, CombatRow.Back)));
Check(!loadout.ValidTarget(slash, new(false, true, CombatTeam.Enemies, CombatRow.Front)));
Check(!loadout.ValidTarget(slash, new(true, false, CombatTeam.Enemies, CombatRow.Front)));
Check(!loadout.ValidTarget(slash, new(true, true, CombatTeam.PlayerAndAllies, CombatRow.Front)));
var invalidCost = new CommandDefinition("bad-cost", "Bad cost", float.NaN, RequiresTarget: false);
loadout.Abilities.Add(invalidCost);
Check(!loadout.Confirm(commandsCombat, "player", invalidCost, () => null, () => { }));
commandsCombat.End();
pendingLoadout = loadout.BeginEquipment();
pendingLoadout["Right hand"] = "sword";
Check(!loadout.Commit(commandsCombat, "player", pendingLoadout));
Check(loadout.Equipment.Count == 0);

// Accessory artwork does not impose a ring/belt/neck equipment type.
var accessoryLoadout = new CombatLoadout();
var charm = new CarriedItem("charm", "Charm", "Accessory");
accessoryLoadout.Inventory.Add(charm);
foreach (var slot in new[] { "Acc1", "Acc2", "Acc3", "Acc4" }) Check(accessoryLoadout.CanEquip(charm, slot));
Check(!accessoryLoadout.CanEquip(charm, "Head"));
Check(!accessoryLoadout.CanEquip(charm, "Ring"));
var accessoryCombat = new CombatSystem();
accessoryCombat.Add(new Combatant("player", true));
accessoryCombat.Process();
var accessoryPending = accessoryLoadout.BeginEquipment();
accessoryPending["Acc4"] = "charm";
Check(accessoryLoadout.Commit(accessoryCombat, "player", accessoryPending));
Check(accessoryLoadout.Equipment["Acc4"] == "charm");
accessoryPending = accessoryLoadout.BeginEquipment();
accessoryPending["Acc1"] = "charm";
Check(!accessoryLoadout.Commit(accessoryCombat, "player", accessoryPending));
Console.WriteLine("Generic accessory slot checks passed.");

// Costs reflect final changed slots, and one confirmation advances Turn once.
accessoryPending.Remove("Acc4");
Check(accessoryLoadout.ChangedEquipmentSlots(accessoryPending).Count == 2);
Check(Math.Abs(accessoryLoadout.Cost(accessoryPending) - 1.2f) < 0.0001f);
var beforeMove = accessoryCombat.ActiveCombatant!.Turn;
Check(accessoryLoadout.Commit(accessoryCombat, "player", accessoryPending));
Check(Math.Abs(accessoryCombat.ActiveCombatant!.Turn - beforeMove - 1.2f) < 0.0001f);
Check(accessoryLoadout.Equipment.Count == 1 && accessoryLoadout.Equipment["Acc1"] == "charm");
var unchanged = accessoryLoadout.BeginEquipment();
unchanged.Remove("Acc1");
Check(accessoryLoadout.Cost(unchanged) == 0.6f);
unchanged["Acc1"] = "charm";
Check(accessoryLoadout.ChangedEquipmentSlots(unchanged).Count == 0);
var beforeUnchanged = accessoryCombat.ActiveCombatant!.Turn;
Check(accessoryLoadout.Commit(accessoryCombat, "player", unchanged));
Check(accessoryCombat.ActiveCombatant!.Turn == beforeUnchanged);
accessoryLoadout.Inventory.Add(new("other-charm", "Other charm", "Accessory"));
unchanged["Acc1"] = "other-charm";
Check(accessoryLoadout.ChangedEquipmentSlots(unchanged).Count == 1);
Check(accessoryLoadout.Cost(unchanged) == 0.6f);
Check(accessoryLoadout.Equipment["Acc1"] == "charm");
Console.WriteLine("Per-slot equipment cost checks passed.");

Check(loadout.QuickSlots.Length == 10);
Check(loadout.Assign(5, "wait") && loadout.QuickSlots[5] == "wait");
Check(!loadout.Assign(10, "wait"));
Console.WriteLine("Ten action slot checks passed.");

Check(loadout.CanSelectQuickSlot(5));
Check(!loadout.CanSelectQuickSlot(4) && !loadout.CanSelectQuickSlot(10));
var placeholder = new CommandDefinition("placeholder", "Placeholder", 0f);
loadout.Abilities.Add(placeholder);
Check(loadout.Assign(4, "placeholder") && !loadout.CanSelectQuickSlot(4));
Check(!loadout.CanSelectQuickSlot(0)); // Slash no longer has its required equipment or focus.
Console.WriteLine("Unavailable action selection checks passed.");

Check(loadout.Assign(9, "wait"));
Check(loadout.QuickSlots[5] is null && loadout.QuickSlots[9] == "wait");
Check(loadout.QuickSlots.Count(id => id == "wait") == 1);
Check(loadout.Assign(9, "wait") && loadout.QuickSlots[9] == "wait");
Check(!loadout.Assign(10, "wait") && loadout.QuickSlots[9] == "wait");
Check(!loadout.Assign(9, "locked") && loadout.QuickSlots[9] == "wait");
Check(loadout.Assign(4, "wait"));
Check(loadout.QuickSlots[9] is null && loadout.QuickSlots[4] == "wait");
Check(loadout.QuickSlots.All(id => id != "placeholder"));
Console.WriteLine("Unique action assignment checks passed.");

// Equipment drops edit the pending layout, and exchange items across compatible slots.
var dragLoadout = new CombatLoadout();
dragLoadout.Inventory.AddRange([
    new("charm-a", "Charm A", "Accessory"),
    new("charm-b", "Charm B", "Accessory"),
    new("charm-c", "Charm C", "Accessory"),
    new("hat", "Hat", "Head")]);
var dragPending = dragLoadout.BeginEquipment();
Check(dragLoadout.TryEquip(dragPending, "charm-a", "Acc1"));
Check(dragLoadout.TryEquip(dragPending, "charm-b", "Acc2"));
Check(dragLoadout.TryEquip(dragPending, "charm-a", "Acc2"));
Check(dragPending["Acc1"] == "charm-b" && dragPending["Acc2"] == "charm-a");
Check(dragLoadout.TryEquip(dragPending, "charm-c", "Acc2"));
Check(dragPending["Acc2"] == "charm-c" && !dragPending.Values.Contains("charm-a"));
Check(dragLoadout.Inventory.Any(i => i.Id.Value == "charm-a"));
var beforeInvalidDrop = new Dictionary<string, string>(dragPending);
Check(!dragLoadout.TryEquip(dragPending, "hat", "Acc1"));
Check(!dragLoadout.TryEquip(dragPending, "missing", "Head"));
Check(!dragLoadout.TryEquip(dragPending, "hat", "invalid-slot"));
Check(dragPending.Count == beforeInvalidDrop.Count && beforeInvalidDrop.All(p => dragPending[p.Key] == p.Value));
Check(dragLoadout.TryEquip(dragPending, "charm-c", "Acc4"));
Check(!dragPending.ContainsKey("Acc2") && dragPending["Acc4"] == "charm-c");
Check(dragLoadout.Equipment.Count == 0); // Cancel leaves committed state untouched.
Check(dragLoadout.TryEquip(dragPending, "charm-c", "Acc4"));
Check(dragPending.Values.Distinct().Count() == dragPending.Count);
Console.WriteLine("Equipment drag swap checks passed.");

// Basic hand strikes are intrinsic commands, independent of learned abilities and held item identity.
var hands = new CombatLoadout();
var handCombat = new CombatSystem();
handCombat.Add(new("player", true));
handCombat.Process();
Check(hands.Abilities.Count == 0);
Check(hands.Assign(0, "LeftHandStrike") && hands.Assign(1, "RightHandStrike"));
Check(hands.CanSelectQuickSlot(0) && hands.CanSelectQuickSlot(1));
var handTarget = new CommandTarget(true, true, CombatTeam.Enemies, CombatRow.Front);
HandStrike? lastStrike = null;
var handHits = 0;
void Strike(HandStrike strike) { lastStrike = strike; handHits++; }
var handTurn = handCombat.ActiveCombatant!.Turn;
Check(!hands.Confirm(handCombat, "player", CombatLoadout.LeftHandStrike, () => null, () => { }, Strike));
Check(!hands.Confirm(handCombat, "player", CombatLoadout.LeftHandStrike, () => handTarget, () => { }));
Check(handHits == 0 && handCombat.ActiveCombatant!.Turn == handTurn);
Check(hands.Confirm(handCombat, "player", CombatLoadout.LeftHandStrike, () => handTarget, () => { }, Strike));
Check(lastStrike is { Hand: StrikeHand.Left, Item: null, Damage: 1 });
Check(handCombat.ActiveCombatant!.Turn == handTurn + 1f && hands.Focus == 0);
hands.Inventory.Add(new("apple", "Apple", "Right hand", ["apple-icon"]));
var handPending = hands.BeginEquipment();
Check(hands.TryEquip(handPending, "apple", "Right hand"));
Check(hands.Commit(handCombat, "player", handPending));
Check(hands.QuickSlots[1] == "RightHandStrike");
Check(hands.CommandIcon(CombatLoadout.RightHandStrike) == "apple-icon");
Check(hands.Confirm(handCombat, "player", CombatLoadout.RightHandStrike, () => handTarget, () => { }, Strike));
Check(lastStrike is { Hand: StrikeHand.Right, Item.Name: "Apple", Damage: 1 });
handPending.Clear();
Check(hands.Commit(handCombat, "player", handPending));
Check(hands.QuickSlots[1] == "RightHandStrike" && hands.CanSelectQuickSlot(1));
Check(hands.CommandIcon(CombatLoadout.RightHandStrike) == "actions.fist.png");
Check(hands.Confirm(handCombat, "player", CombatLoadout.RightHandStrike, () => handTarget, () => { }, Strike));
Check(lastStrike is { Hand: StrikeHand.Right, Item: null } && handHits == 3);
Check(hands.Assign(9, "RightHandStrike") && hands.QuickSlots[1] is null);
Console.WriteLine("Intrinsic hand strike checks passed.");

// Each damage point has one type; the basic point remains General.
Check(lastStrike!.DamagePoints.Typed.GetValueOrDefault(DamageType.General) == 1 && lastStrike.DamagePoints.Typed.Count == 1);
var scimitarBonus = new Dictionary<DamageType, int> { [DamageType.General] = 1, [DamageType.Slashing] = 2 };
var scimitar = new WeaponData { Damage = scimitarBonus };
Check(new ItemData { Weapon = scimitar }.Weapon == scimitar && new ItemData().Weapon is null);
hands.Inventory.Add(new("scimitar", "Scimitar", "Left hand", Weapon: scimitar));
handPending = hands.BeginEquipment();
Check(hands.TryEquip(handPending, "scimitar", "Left hand"));
Check(hands.Commit(handCombat, "player", handPending));
var typedHits = 0;
var typedDamage = 0;
Check(hands.Confirm(handCombat, "player", CombatLoadout.LeftHandStrike, () => handTarget, () => { }, strike =>
{
    lastStrike = strike;
    typedHits++;
    typedDamage += strike.Damage;
}));
Check(typedHits == 1 && typedDamage == 3);
Check(lastStrike!.DamagePoints.Typed.GetValueOrDefault(DamageType.General) == 1 && lastStrike.DamagePoints.Typed[DamageType.Slashing] == 2);
scimitarBonus[DamageType.Slashing] = 9;
Check(lastStrike.Damage == 3); // Resolved points are a snapshot.
scimitarBonus[DamageType.Slashing] = 2;
var sharp = new HandStrike(StrikeHand.Left, new("sharp", "Sharp scimitar", "Left hand", Weapon: scimitar with { Sharp = true }));
Check(sharp.Damage == 4 && sharp.DamagePoints.Typed[DamageType.Slashing] == 3);
Check(sharp.DamagePoints.Negate(DamageType.Slashing).Total == 1);
Check(sharp.DamagePoints.Reduce(DamageType.Slashing, 1).Total == 3);
Check(sharp.DamagePoints.Reduce(DamageType.Slashing, 99).Total == 1);
Check(lastStrike.DamagePoints.Negate(DamageType.Slashing).Total == 1);
var mace = new HandStrike(StrikeHand.Right, new("mace", "Spiked mace", "Right hand", Weapon: new()
{
    Damage = new Dictionary<DamageType, int> { [DamageType.General] = 1, [DamageType.Crushing] = 1, [DamageType.Piercing] = 1 },
}));
Check(mace.Damage == 3 && mace.DamagePoints.Typed.GetValueOrDefault(DamageType.General) == 1);
Check(mace.DamagePoints.Negate(DamageType.Crushing).Total == 2);
Check(mace.DamagePoints.Negate(DamageType.Crushing).Negate(DamageType.Piercing).Total == 1);
var appleStrike = new HandStrike(StrikeHand.Right, hands.Inventory.Find(i => i.Id.Value == "apple"));
Check(appleStrike.Damage == 1 && appleStrike.DamagePoints.Typed.Count == 1);
Check(DamageProfile.ForStrike(new WeaponData()).Total == 1);
try { _ = new DamageProfile(new Dictionary<DamageType, int> { [DamageType.Slashing] = -1 }); throw new Exception("Accepted negative damage"); }
catch (ArgumentOutOfRangeException) { }
Console.WriteLine("Atomic damage point checks passed.");
var weaponExpectations = new[]
{
    (WeaponType.Sword, 2, 0, 0, 7),
    (WeaponType.Axe, 1, 1, 0, 5),
    (WeaponType.Knives, 1, 0, 1, 5),
    (WeaponType.Clubs, 0, 2, 0, 3),
    (WeaponType.Maces, 0, 1, 1, 2),
    (WeaponType.Flail, 0, 1, 1, 2),
};
foreach (var (type, slashing, crushing, piercing, iconCount) in weaponExpectations)
{
    var data = WeaponCatalog.Get(type);
    Check(data.Icons.Length == iconCount);
    for (var index = 0; index < iconCount; index++)
    {
        var item = WeaponCatalog.Create(type, "catalog-weapon", type.ToString(), "Left hand", index);
        var strike = new HandStrike(StrikeHand.Left, item);
        Check(item.Icon == data.Icons[index]);
        Check(strike.Damage == 3 && strike.DamagePoints.Typed.GetValueOrDefault(DamageType.General) == 1);
        Check(strike.DamagePoints.Typed.GetValueOrDefault(DamageType.Slashing) == slashing);
        Check(strike.DamagePoints.Typed.GetValueOrDefault(DamageType.Crushing) == crushing);
        Check(strike.DamagePoints.Typed.GetValueOrDefault(DamageType.Piercing) == piercing);
        Check(DamageProfile.ForStrike(item.Weapon! with { Sharp = true }).Total == (slashing > 0 ? 4 : 3));
    }
}
Check(new HandStrike(StrikeHand.Right, CombatLoadout.CreatePlayerLoadout().HeldItem(StrikeHand.Right)).Damage == 3);
Console.WriteLine("Weapon catalog and variant damage checks passed.");

foreach (var type in Enum.GetValues<DamageType>()) Check(type.GetCategory() == (type == DamageType.General ? null : DamageCategory.Physical));
Check(mace.DamagePoints.AmountIn(DamageCategory.Physical) == 2);
Check(mace.DamagePoints.Total == 3); // The General point belongs to no category.
foreach (var category in new[] { DamageCategory.Elemental, DamageCategory.Spiritual, DamageCategory.Mental, DamageCategory.Magical })
    Check(mace.DamagePoints.AmountIn(category) == 0);
Check(appleStrike.DamagePoints.AmountIn(DamageCategory.Physical) == 0);
Console.WriteLine("Damage category checks passed.");

var startingLoadout = CombatLoadout.CreatePlayerLoadout();
Check(startingLoadout.HeldItem(StrikeHand.Right) is { Name: "Sword", Weapon: not null });
Check(startingLoadout.HeldItem(StrikeHand.Left) is null);
Check(startingLoadout.Equipment.Count == 1);
Check(startingLoadout.QuickSlots[0] == "RightHandStrike" && startingLoadout.QuickSlots[1] == "LeftHandStrike");
Check(startingLoadout.QuickSlots[2] == "Throw" && startingLoadout.QuickSlots.Skip(3).Take(6).All(slot => slot is null));
Check(startingLoadout.QuickSlots[9] == "wait");
Check(startingLoadout.CanSelectQuickSlot(0) && startingLoadout.CanSelectQuickSlot(1));
Check(startingLoadout.Cost(startingLoadout.BeginEquipment()) == 0f);
var otherStartingLoadout = CombatLoadout.CreatePlayerLoadout();
startingLoadout.Inventory.Clear();
Check(otherStartingLoadout.Inventory.Count == 1 && otherStartingLoadout.Equipment.Count == 1);
Console.WriteLine("Starting player loadout checks passed.");

// Range checks cover every nonempty combination of occupied enemy rows.
foreach (var range in Enum.GetValues<WeaponRange>())
{
    var ranged = new CombatLoadout();
    ranged.Inventory.Add(new("ranged", "Ranged", "Right hand", Weapon: new() { Range = range }));
    var rangedCombat = new CombatSystem();
    rangedCombat.Add(new("player", true));
    rangedCombat.Process();
    var rangedPending = ranged.BeginEquipment();
    rangedPending["Right hand"] = "ranged";
    Check(ranged.Commit(rangedCombat, "player", rangedPending));
    for (var mask = 1; mask < 8; mask++)
    {
        var firstOccupied = Enumerable.Range(0, 3).First(row => (mask & (1 << row)) != 0);
        foreach (var row in Enum.GetValues<CombatRow>())
        {
            if ((mask & (1 << (int)row)) == 0) continue;
            var target = new CommandTarget(true, true, CombatTeam.Enemies, row,
                (mask & 1) != 0, (mask & 2) != 0);
            var expected = range == WeaponRange.Long
                || range == WeaponRange.Short && (int)row == firstOccupied
                || range == WeaponRange.Medium && (row != CombatRow.Back || (mask & 3) == 0);
            Check(ranged.ValidTarget(CombatLoadout.RightHandStrike, target) == expected);
        }
    }
    Check(!ranged.ValidTarget(CombatLoadout.RightHandStrike, new(true, true, CombatTeam.PlayerAndAllies, CombatRow.Front)));
    Check(!ranged.ValidTarget(CombatLoadout.RightHandStrike, new(true, false, CombatTeam.Enemies, CombatRow.Front)));
}
var unarmedRange = new CombatLoadout();
Check(unarmedRange.ValidTarget(CombatLoadout.LeftHandStrike, new(true, true, CombatTeam.Enemies, CombatRow.Back, false, false)));
Check(!unarmedRange.ValidTarget(CombatLoadout.LeftHandStrike, new(true, true, CombatTeam.Enemies, CombatRow.Back, false, true)));
Console.WriteLine("Weapon range checks passed.");

var variantWeapon = new WeaponData { Range = WeaponRange.Long };
var variantItem = new ItemData { Icons = ["firearm-a", "firearm-b"], Weapon = variantWeapon };
Check(variantItem.Icon == "firearm-a");
Check((variantItem with { IconIndex = 1 }).Icon == "firearm-b");
Check((variantItem with { IconIndex = 99 }).Icon == "firearm-b");
Check((variantItem with { IconIndex = -1 }).Icon == "firearm-b");
Check(new ItemData().Icon is null);
var carriedVariant = new CarriedItem("firearm", "Firearm", "Right hand", variantItem.Icons, variantWeapon) { IconIndex = 1 };
var variants = new CombatLoadout();
variants.Inventory.Add(carriedVariant);
var variantCombat = new CombatSystem();
variantCombat.Add(new("player", true));
variantCombat.Process();
var variantPending = variants.BeginEquipment();
Check(variants.TryEquip(variantPending, "firearm", "Right hand"));
Check(variants.Commit(variantCombat, "player", variantPending));
Check(variants.CommandIcon(CombatLoadout.RightHandStrike) == "firearm-b");
Check(carriedVariant.Icon == "firearm-b" && ReferenceEquals(carriedVariant.Weapon, variantWeapon));
Check((carriedVariant with { IconIndex = 0 }).Icon == "firearm-a");
Check((carriedVariant with { IconIndex = 0 }).Weapon == carriedVariant.Weapon);
Check(new CarriedItem("none", "None", "Head").Icon is null);
Console.WriteLine("Item icon variant checks passed.");

// Outcomes stop the encounter once, with defeat taking priority over victory/escape.
var victoryCombat = new CombatSystem();
victoryCombat.Add(new("player", true));
victoryCombat.Add(new("enemy", false, turn: 0.5f, team: CombatTeam.Enemies));
victoryCombat.DecideAction = (_, _) => new(0.6f, (_, _) => { });
var objectiveMet = false;
victoryCombat.VictoryConditions = [system => !system.Contains("enemy"), _ => objectiveMet];
var outcomeNotifications = 0;
victoryCombat.Ended += outcome => { Check(outcome == CombatOutcome.Victory); outcomeNotifications++; };
victoryCombat.Process();
victoryCombat.SubmitAction(new(1f, (system, _) => system.Remove("enemy")));
Check(!victoryCombat.HasEnded); // All victory conditions are required.
objectiveMet = true;
Check(victoryCombat.EvaluateOutcome());
Check(victoryCombat.Outcome == CombatOutcome.Victory && victoryCombat.ActiveCombatant is null);
Check(victoryCombat.Timeline.TurnOrder.Count == 0);
victoryCombat.EvaluateOutcome();
victoryCombat.End(CombatOutcome.Defeat);
Check(outcomeNotifications == 1 && victoryCombat.Outcome == CombatOutcome.Victory);

var escapeCombat = new CombatSystem();
escapeCombat.Add(new("player", true));
escapeCombat.Add(new("enemy", false, turn: 0.2f, team: CombatTeam.Enemies));
var enemyTurns = 0;
escapeCombat.DecideAction = (_, _) => new(0.4f, (_, _) => enemyTurns++);
escapeCombat.Process();
Check(!escapeCombat.RequestEscape("other"));
Check(escapeCombat.RequestEscape("player"));
Check(escapeCombat.Outcome == CombatOutcome.Escape && enemyTurns == 2);
Check(escapeCombat.Timeline.CurrentTurn == 1f && escapeCombat.ActiveCombatant is null);
Check(!escapeCombat.RequestEscape("player"));

var interruptedEscape = new CombatSystem();
interruptedEscape.Add(new("player", true));
interruptedEscape.DefeatConditions = [system => !system.Contains("player")];
interruptedEscape.ScheduleEvent(1f, "defeated before fleeing", system => system.Remove("player"));
interruptedEscape.Process();
Check(interruptedEscape.RequestEscape("player"));
Check(interruptedEscape.Outcome == CombatOutcome.Defeat);
var bothConditions = new CombatSystem();
bothConditions.VictoryConditions = [_ => true];
bothConditions.DefeatConditions = [_ => true];
bothConditions.Process();
Check(bothConditions.Outcome == CombatOutcome.Defeat);
Console.WriteLine("Combat outcome and delayed escape checks passed.");

// Every actor's resolved turn checks scenario conditions before the next occurrence.
var turnOutcome = new CombatSystem();
turnOutcome.Add(new("player", true));
turnOutcome.Add(new("enemy", false, turn: 0.25f, team: CombatTeam.Enemies));
var turnObjective = false;
turnOutcome.VictoryConditions = [_ => turnObjective];
turnOutcome.DecideAction = (_, _) => new(0.5f, (_, _) => turnObjective = true);
turnOutcome.Process();
turnOutcome.SubmitAction(new(1f, (_, _) => { }));
Check(turnOutcome.Outcome == CombatOutcome.Victory && turnOutcome.Timeline.CurrentTurn == 0.25f);
var healthOutcome = new CombatSystem();
healthOutcome.Add(new("player", true));
healthOutcome.Add(new("enemy", false, turn: 0.25f, team: CombatTeam.Enemies));
var playerAlive = true;
healthOutcome.IsCombatantAlive = id => id != "player" || playerAlive;
healthOutcome.VictoryConditions = [CombatConditions.AllEnemiesDefeated];
healthOutcome.DefeatConditions = [CombatConditions.PlayerDefeated];
healthOutcome.DecideAction = (_, _) => new(0.5f, (_, _) => playerAlive = false);
healthOutcome.Process();
healthOutcome.SubmitAction(new(1f, (_, _) => { }));
Check(healthOutcome.Outcome == CombatOutcome.Defeat);
Console.WriteLine("Per-turn scenario condition checks passed.");

var leaderCombat = new CombatSystem();
leaderCombat.Add(new("player", true));
leaderCombat.Add(new("leader", false, turn: 2f, team: CombatTeam.Enemies));
leaderCombat.Add(new("guard", false, turn: 2f, team: CombatTeam.Enemies));
leaderCombat.VictoryConditions = [CombatConditions.CharacterDefeated("leader")];
leaderCombat.Process();
leaderCombat.SubmitAction(new(1f, (system, _) => system.Remove("leader")));
Check(leaderCombat.Outcome == CombatOutcome.Victory && leaderCombat.Contains("guard"));
Console.WriteLine("Designated character victory checks passed.");

var fistDefaults = CombatLoadout.CreatePlayerLoadout();
Check(fistDefaults.CommandIcon(fistDefaults.Find(fistDefaults.QuickSlots[1])!) == "actions.fist.png");
fistDefaults.Inventory.Add(new("blank", "Blank icon", "Left hand", [""]));
var fistCombat = new CombatSystem();
fistCombat.Add(new("player", true));
fistCombat.Process();
var fistPending = fistDefaults.BeginEquipment();
fistPending["Left hand"] = "blank";
Check(fistDefaults.Commit(fistCombat, "player", fistPending));
Check(fistDefaults.CommandIcon(CombatLoadout.LeftHandStrike) == "actions.fist.png");
Console.WriteLine("Empty hand fist icon checks passed.");

var pistolItem = WeaponCatalog.Create(WeaponType.Pistol, "pistol", "Pistol", "Right hand");
var pistolStrike = new HandStrike(StrikeHand.Right, pistolItem);
Check(pistolItem.Weapon!.Range == WeaponRange.Long);
Check(pistolStrike.Damage == 3 && pistolStrike.DamagePoints.Typed.Count == 1);
Check(pistolStrike.DamagePoints.Typed[DamageType.Piercing] == 3);
Check(pistolStrike.DamagePoints.Negate(DamageType.Piercing).Total == 0);
Check(pistolStrike.DamagePoints.Reduce(DamageType.Piercing, 1).Total == 2);
Check(DamageProfile.ForStrike(null).Negate(DamageType.General).Total == 0);
Check(DamageProfile.ForStrike(null).Reduce(DamageType.General, 1).Total == 0);
var pistolLoadout = new CombatLoadout();
pistolLoadout.Inventory.Add(pistolItem);
var pistolPending = pistolLoadout.BeginEquipment();
Check(pistolLoadout.TryEquip(pistolPending, "pistol", "Right hand"));
var pistolCombat = new CombatSystem();
pistolCombat.Add(new("player", true));
pistolCombat.Process();
Check(pistolLoadout.Commit(pistolCombat, "player", pistolPending));
Check(pistolLoadout.ValidTarget(CombatLoadout.RightHandStrike, new(true, true, CombatTeam.Enemies, CombatRow.Back, true, true)));
Console.WriteLine("General damage and Pistol checks passed.");
// Throw selection is read-only until a valid, single-use commit.
CombatSystem ThrowCombat()
{
    var system = new CombatSystem();
    system.Add(new("player", true));
    system.Process();
    return system;
}
var throwTarget = new CommandTarget(true, true, CombatTeam.Enemies, CombatRow.Back);
var throws = new CombatLoadout();
var throwingSword = WeaponCatalog.Create(WeaponType.Sword, "throw-sword", "Sword", "Right hand");
throws.Inventory.Add(throwingSword);
var throwCombat = ThrowCombat();
var throwEquip = throws.BeginEquipment();
throwEquip["Right hand"] = "throw-sword";
Check(throws.Commit(throwCombat, "player", throwEquip));
Check(!throws.ValidTarget(CombatLoadout.RightHandStrike, throwTarget));
Check(throws.ValidTarget(CombatLoadout.Throw, throwTarget));
var beforeThrow = throwCombat.Timeline.CurrentTurn;
var cancelledThrow = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
Check(cancelledThrow.Options.Count == 1 && throws.Inventory.Count == 1);
cancelledThrow.Cancel();
Check(!cancelledThrow.Choose(cancelledThrow.Options[0], _ => throw new Exception()));
Check(throwCombat.Timeline.CurrentTurn == beforeThrow && throws.Equipment.ContainsKey("Right hand"));
var throwSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
var throwDamage = 0;
Check(throwSession.Choose(throwSession.Options[0], profile => throwDamage += profile.Total));
Check(throwDamage == 1 && DamageProfile.ForStrike(throwingSword.Weapon).Total != throwDamage);
Check(!throwSession.Choose(throwSession.Options[0], _ => throwDamage++));
Check(throws.Inventory.Count == 0 && throws.Equipment.Count == 0 && throws.ThrownItems.Single().Id == throwingSword.Id);
Check(throwCombat.Timeline.CurrentTurn == beforeThrow + CombatLoadout.Throw.TurnCost);
Check(!throws.Available(CombatLoadout.Throw));

var stack = new CarriedItem("stones", "Stones", "Accessory")
    { Quantity = 3, ThrowDamage = new(new Dictionary<DamageType, int> { [DamageType.General] = 1 }) };
throws.Inventory.Add(stack);
var stackSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
Check(stackSession.Choose(stackSession.Options.Single(), profile => Check(profile.Total == 1)));
Check(throws.Inventory.Single().Quantity == 2 && throws.Equipment.Count == 0);

var twoHanded = throwingSword with { Id = "two-hand", Weapon = throwingSword.Weapon! with { TwoHanded = true } };
throws.Inventory.Add(twoHanded);
var bothHands = throws.BeginEquipment();
Check(throws.TryEquip(bothHands, "two-hand", "Right hand"));
Check(throws.Commit(throwCombat, "player", bothHands));
Check(!throws.HasFreeHand && throws.ThrowOptions().Single().Item == twoHanded);
var twoHandSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
Check(twoHandSession.Choose(twoHandSession.Options.Single(), _ => { }));
Check(throws.Equipment.Count == 0);

var staleHands = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
throws.Inventory.Add(new("shield-left", "Shield", "Left hand"));
throws.Inventory.Add(new("shield-right", "Shield", "Right hand"));
var shields = throws.BeginEquipment();
shields["Left hand"] = "shield-left"; shields["Right hand"] = "shield-right";
Check(throws.Commit(throwCombat, "player", shields));
var staleTurn = throwCombat.Timeline.CurrentTurn;
Check(!staleHands.Choose(staleHands.Options.Single(), _ => throw new Exception()));
Check(throws.Inventory.First().Quantity == 2 && throwCombat.Timeline.CurrentTurn == staleTurn);
Check(!throws.Available(CombatLoadout.Throw));
Check(throws.Commit(throwCombat, "player", []));
var currentThrowTarget = throwTarget;
var invalidTargetSession = throws.BeginThrow(throwCombat, "player", () => currentThrowTarget)!;
currentThrowTarget = throwTarget with { Alive = false };
Check(!invalidTargetSession.Choose(invalidTargetSession.Options.Single(), _ => throw new Exception()));
var ownershipSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
throws.Inventory.RemoveAt(0);
Check(!ownershipSession.Choose(ownershipSession.Options.Single(), _ => throw new Exception()));
Console.WriteLine("Throw targeting, cancellation, stacks, hands, stale state and single submission checks passed.");

// Wait yields to the next living character, rather than a fixed duration or an effect.
var waiting = CombatLoadout.CreatePlayerLoadout();
Check(waiting.CommandIcon(CombatLoadout.Wait) == "actions.wait.png");
foreach (var nextTurn in new[] { 0f, 0.2f, 4f })
{
    var system = new CombatSystem();
    system.Add(new("player", true, initiative: 100));
    system.Add(new("next", false, turn: nextTurn, team: CombatTeam.Enemies));
    system.Add(new("later", false, turn: nextTurn + 2f, team: CombatTeam.Enemies));
    var acted = false;
    var effect = false;
    system.DecideAction = (_, actor) => new(1f, (_, _) => { Check(actor.Id == "next"); acted = true; });
    system.ScheduleEvent(nextTurn / 2f, "Earlier effect", _ => effect = true);
    system.Process();
    Check(waiting.Confirm(system, "player", CombatLoadout.Wait, () => null, () => { }));
    Check(acted && effect && system.ActiveCombatant?.Id == "player");
    Check(Math.Abs(system.ActiveCombatant!.Turn - (nextTurn + 0.01f)) < 0.00001f);
}
var waitLive = new CombatSystem();
waitLive.Add(new("player", true));
waitLive.Add(new("dead", false, turn: 0.1f));
waitLive.Add(new("ally", false, turn: 2f));
waitLive.IsCombatantAlive = id => id != "dead";
waitLive.Process();
Check(Math.Abs(waitLive.WaitCost("player")!.Value - 2.01f) < 0.00001f);
waitLive.AdjustTurn("ally", 3f);
Check(Math.Abs(waitLive.WaitCost("player")!.Value - 5.01f) < 0.00001f);
waitLive.Remove("ally");
Check(waitLive.WaitCost("player") is null);
Check(!waiting.CanConfirm(waitLive, "player", CombatLoadout.Wait, true));
Check(!waiting.Confirm(waitLive, "player", CombatLoadout.Wait, () => null, () => throw new Exception()));
Check(waitLive.ActiveCombatant?.Id == "player" && waitLive.Timeline.CurrentTurn == 0f);
Console.WriteLine("Wait dynamic timing, character-only selection, live updates and unavailable-wait checks passed.");

// Wait never bypasses normal ordering when several characters share the next Turn.
var tiedWait = new CombatSystem();
tiedWait.Add(new("player", true, initiative: 100));
tiedWait.Add(new("tied-low", false, turn: 1f, initiative: 1, team: CombatTeam.Enemies));
tiedWait.Add(new("tied-high", false, turn: 1f, initiative: 5, team: CombatTeam.Enemies));
tiedWait.Add(new("nearby", false, turn: 1.005f, team: CombatTeam.Enemies));
var waitedActors = new List<string>();
tiedWait.DecideAction = (_, actor) => new(2f, (_, _) => waitedActors.Add(actor.Id));
tiedWait.Process();
Check(waiting.Confirm(tiedWait, "player", CombatLoadout.Wait, () => null, () => { }));
Check(waitedActors.SequenceEqual(new[] { "tied-high", "tied-low", "nearby" }));
Check(tiedWait.ActiveCombatant?.Id == "player" && Math.Abs(tiedWait.ActiveCombatant.Turn - 1.01f) < 0.00001f);
Console.WriteLine("Wait preserves normal ordering across tied and nearby character Turns.");
