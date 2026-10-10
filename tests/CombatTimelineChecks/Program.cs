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
hands.Inventory.Add(new("apple", "Apple", "Right hand", "apple-icon"));
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
Check(hands.CommandIcon(CombatLoadout.RightHandStrike) == "stats.fist.png");
Check(hands.Confirm(handCombat, "player", CombatLoadout.RightHandStrike, () => handTarget, () => { }, Strike));
Check(lastStrike is { Hand: StrikeHand.Right, Item: null } && handHits == 3);
Check(hands.Assign(9, "RightHandStrike") && hands.QuickSlots[1] is null);
Console.WriteLine("Intrinsic hand strike checks passed.");

// Each damage point has one type; the basic point remains untyped.
Check(lastStrike!.DamagePoints.Untyped == 1 && lastStrike.DamagePoints.Typed.Count == 0);
var scimitarBonus = new Dictionary<DamageType, int> { [DamageType.Slashing] = 1 };
var scimitar = new WeaponData { BonusDamage = scimitarBonus };
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
Check(typedHits == 1 && typedDamage == 2);
Check(lastStrike!.DamagePoints.Untyped == 1 && lastStrike.DamagePoints.Typed[DamageType.Slashing] == 1);
scimitarBonus[DamageType.Slashing] = 9;
Check(lastStrike.Damage == 2); // Resolved points are a snapshot.
scimitarBonus[DamageType.Slashing] = 1;
var sharp = new HandStrike(StrikeHand.Left, new("sharp", "Sharp scimitar", "Left hand", Weapon: scimitar with { Sharp = true }));
Check(sharp.Damage == 3 && sharp.DamagePoints.Typed[DamageType.Slashing] == 2);
Check(sharp.DamagePoints.Negate(DamageType.Slashing).Total == 1);
Check(sharp.DamagePoints.Reduce(DamageType.Slashing, 1).Total == 2);
Check(sharp.DamagePoints.Reduce(DamageType.Slashing, 99).Total == 1);
Check(lastStrike.DamagePoints.Negate(DamageType.Slashing).Total == 1);
var mace = new HandStrike(StrikeHand.Right, new("mace", "Spiked mace", "Right hand", Weapon: new()
{
    BonusDamage = new Dictionary<DamageType, int> { [DamageType.Crushing] = 1, [DamageType.Piercing] = 1 },
}));
Check(mace.Damage == 3 && mace.DamagePoints.Untyped == 1);
Check(mace.DamagePoints.Negate(DamageType.Crushing).Total == 2);
Check(mace.DamagePoints.Negate(DamageType.Crushing).Negate(DamageType.Piercing).Total == 1);
var appleStrike = new HandStrike(StrikeHand.Right, hands.Inventory.Find(i => i.Id.Value == "apple"));
Check(appleStrike.Damage == 1 && appleStrike.DamagePoints.Typed.Count == 0);
Check(DamageProfile.ForStrike(new WeaponData()).Total == 1);
try { _ = new DamageProfile(1, new Dictionary<DamageType, int> { [DamageType.Slashing] = -1 }); throw new Exception("Accepted negative damage"); }
catch (ArgumentOutOfRangeException) { }
Console.WriteLine("Atomic damage point checks passed.");

foreach (var type in Enum.GetValues<DamageType>()) Check(type.GetCategory() == DamageCategory.Physical);
Check(mace.DamagePoints.AmountIn(DamageCategory.Physical) == 2);
Check(mace.DamagePoints.Total == 3); // The untyped point belongs to no category.
foreach (var category in new[] { DamageCategory.Elemental, DamageCategory.Spiritual, DamageCategory.Mental, DamageCategory.Magical })
    Check(mace.DamagePoints.AmountIn(category) == 0);
Check(appleStrike.DamagePoints.AmountIn(DamageCategory.Physical) == 0);
Console.WriteLine("Damage category checks passed.");
