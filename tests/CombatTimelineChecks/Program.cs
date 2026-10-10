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
loadout.Items.Add(new("sword", "Sword", "Right hand"));
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
accessoryLoadout.Items.Add(charm);
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
accessoryLoadout.Items.Add(new("other-charm", "Other charm", "Accessory"));
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
dragLoadout.Items.AddRange([
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
Check(dragLoadout.Items.Any(i => i.Id.Value == "charm-a"));
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
hands.Items.Add(new("apple", "Apple", "Right hand", ["apple-icon"]));
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
hands.Items.Add(new("scimitar", "Scimitar", "Left hand", Weapon: scimitar));
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
var appleStrike = new HandStrike(StrikeHand.Right, hands.Items.Find(i => i.Id.Value == "apple"));
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

foreach (var type in new[] { DamageType.Slashing, DamageType.Piercing, DamageType.Crushing, DamageType.Poison })
    Check(type.GetCategory() == DamageCategory.Physical);
foreach (var type in new[] { DamageType.Fire, DamageType.Ice, DamageType.Lightning })
    Check(type.GetCategory() == DamageCategory.Elemental);
Check(DamageType.General.GetCategory() is null);
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
Check(startingLoadout.QuickSlots.Skip(2).Take(7).All(slot => slot is null));
Check(startingLoadout.QuickSlots[9] == "wait");
Check(startingLoadout.CanSelectQuickSlot(0) && startingLoadout.CanSelectQuickSlot(1));
Check(startingLoadout.Cost(startingLoadout.BeginEquipment()) == 0f);
var otherStartingLoadout = CombatLoadout.CreatePlayerLoadout();
startingLoadout.Items.Clear();
Check(otherStartingLoadout.Items.Count == 1 && otherStartingLoadout.Equipment.Count == 1);
Console.WriteLine("Starting player loadout checks passed.");

// Range checks cover every nonempty combination of occupied enemy rows.
foreach (var range in Enum.GetValues<WeaponRange>())
{
    var ranged = new CombatLoadout();
    ranged.Items.Add(new("ranged", "Ranged", "Right hand", Weapon: new() { Range = range }));
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
variants.Items.Add(carriedVariant);
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
fistDefaults.Items.Add(new("blank", "Blank icon", "Left hand", [""]));
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
pistolLoadout.Items.Add(pistolItem);
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
throws.Items.Add(throwingSword);
var throwCombat = ThrowCombat();
var throwEquip = throws.BeginEquipment();
throwEquip["Right hand"] = "throw-sword";
Check(throws.Commit(throwCombat, "player", throwEquip));
Check(!throws.ValidTarget(CombatLoadout.RightHandStrike, throwTarget));
Check(throws.ValidTarget(CombatLoadout.Throw, throwTarget));
var beforeThrow = throwCombat.Timeline.CurrentTurn;
var cancelledThrow = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
Check(cancelledThrow.Options.Count == 1 && throws.Items.Count == 1);
cancelledThrow.Cancel();
Check(!cancelledThrow.Choose(cancelledThrow.Options[0], _ => throw new Exception()));
Check(throwCombat.Timeline.CurrentTurn == beforeThrow && throws.Equipment.ContainsKey("Right hand"));
var throwSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
var throwDamage = 0;
Check(throwSession.Choose(throwSession.Options[0], profile => throwDamage += profile.Total));
Check(throwDamage == 1 && DamageProfile.ForStrike(throwingSword.Weapon).Total != throwDamage);
Check(!throwSession.Choose(throwSession.Options[0], _ => throwDamage++));
Check(throws.Items.Count == 0 && throws.Equipment.Count == 0 && throws.ThrownItems.Single().Id == throwingSword.Id);
Check(throwCombat.Timeline.CurrentTurn == beforeThrow + CombatLoadout.Throw.TurnCost);
Check(!throws.Available(CombatLoadout.Throw));

var stack = new CarriedItem("stones", "Stones", "Accessory")
    { Quantity = 3, ThrowDamage = new(new Dictionary<DamageType, int> { [DamageType.General] = 1 }) };
throws.Items.Add(stack);
var stackSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
Check(stackSession.Choose(stackSession.Options.Single(), profile => Check(profile.Total == 1)));
Check(throws.Items.Single().Quantity == 2 && throws.Equipment.Count == 0);

var twoHanded = throwingSword with { Id = "two-hand", Weapon = throwingSword.Weapon! with { TwoHanded = true } };
throws.Items.Add(twoHanded);
var bothHands = throws.BeginEquipment();
Check(throws.TryEquip(bothHands, "two-hand", "Right hand"));
Check(throws.Commit(throwCombat, "player", bothHands));
Check(!throws.HasFreeHand && throws.ThrowOptions().Single().Item == twoHanded);
var twoHandSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
Check(twoHandSession.Choose(twoHandSession.Options.Single(), _ => { }));
Check(throws.Equipment.Count == 0);

var staleHands = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
throws.Items.Add(new("shield-left", "Shield", "Left hand"));
throws.Items.Add(new("shield-right", "Shield", "Right hand"));
var shields = throws.BeginEquipment();
shields["Left hand"] = "shield-left"; shields["Right hand"] = "shield-right";
Check(throws.Commit(throwCombat, "player", shields));
var staleTurn = throwCombat.Timeline.CurrentTurn;
Check(!staleHands.Choose(staleHands.Options.Single(), _ => throw new Exception()));
Check(throws.Items.First().Quantity == 2 && throwCombat.Timeline.CurrentTurn == staleTurn);
Check(!throws.Available(CombatLoadout.Throw));
Check(throws.Commit(throwCombat, "player", []));
var currentThrowTarget = throwTarget;
var invalidTargetSession = throws.BeginThrow(throwCombat, "player", () => currentThrowTarget)!;
currentThrowTarget = throwTarget with { Alive = false };
Check(!invalidTargetSession.Choose(invalidTargetSession.Options.Single(), _ => throw new Exception()));
var ownershipSession = throws.BeginThrow(throwCombat, "player", () => throwTarget)!;
throws.Items.RemoveAt(0);
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

var pinnedActions = CombatLoadout.CreatePlayerLoadout();
var originalPinnedSlots = pinnedActions.QuickSlots.ToArray();
foreach (var pinnedSlot in new[] { 0, 1, 9 })
{
    Check(pinnedActions.IsQuickSlotPinned(pinnedSlot));
    Check(!pinnedActions.Assign(pinnedSlot, "Throw"));
    Check(pinnedActions.QuickSlots.SequenceEqual(originalPinnedSlots));
    Check(pinnedActions.Assign(pinnedSlot, originalPinnedSlots[pinnedSlot]!));
    Check(!pinnedActions.Assign(4, originalPinnedSlots[pinnedSlot]!));
    Check(pinnedActions.QuickSlots.SequenceEqual(originalPinnedSlots));
}
Check(!pinnedActions.IsQuickSlotPinned(2) && !pinnedActions.IsQuickSlotPinned(8));
Check(!pinnedActions.Assign(8, "Throw") && pinnedActions.QuickSlots[2] is null);
pinnedActions.Abilities.Add(new("extra", "Extra action", 1f));
Check(pinnedActions.Assign(8, "extra"));
Check(pinnedActions.QuickSlots[0] == "RightHandStrike" && pinnedActions.QuickSlots[1] == "LeftHandStrike" && pinnedActions.QuickSlots[9] == "wait");
Console.WriteLine("Pinned player hand attacks and Wait resist replacement and relocation; other slots remain editable.");

// Ranged weapons have independent shoot/reload actions, costs, and per-item ammunition.
foreach (var (type, damage, reloadTime, attackTime, requiresBothHands, icons) in new[]
{
    (WeaponType.Pistol, 3, 1.4f, 0.1f, false, 3),
    (WeaponType.Bow, 3, 0.8f, 0.4f, true, 3),
    (WeaponType.Crossbow, 3, 2.1f, 0.2f, true, 1),
    (WeaponType.Sling, 2, 0.8f, 0.6f, false, 1),
})
foreach (var hand in new[] { StrikeHand.Left, StrikeHand.Right })
{
    var ranged = new CombatLoadout();
    var slot = hand == StrikeHand.Left ? "Left hand" : "Right hand";
    var otherSlot = hand == StrikeHand.Left ? "Right hand" : "Left hand";
    var shoot = hand == StrikeHand.Left ? CombatLoadout.LeftHandStrike : CombatLoadout.RightHandStrike;
    var ammo = WeaponCatalog.CreateAmmunition(weaponTypeAmmo(type), "ammunition", 2);
    var weapon = WeaponCatalog.Create(type, "ranged", type.ToString(), slot);
    ranged.Items.Add(weapon);
    ranged.Items.Add(ammo);
    var system = new CombatSystem();
    var actor = new Combatant("player", true);
    system.Add(actor);
    system.Process();
    var pendingRanged = ranged.BeginEquipment();
    Check(ranged.TryEquip(pendingRanged, weapon.Id.Value, slot));
    Check(pendingRanged.Count == (requiresBothHands ? 2 : 1));
    Check(ranged.Commit(system, "player", pendingRanged));
    Check(weapon.Icons!.Length == icons && weapon.Weapon!.TwoHanded == requiresBothHands);
    Check(ranged.ShotsRemaining(weapon) == 1 && !ranged.CanReload(pendingRanged, weapon.Id.Value, ammo.Id.Value));
    Check(ranged.CommandCost(shoot) == attackTime && weapon.Weapon!.ReloadTime == reloadTime);
    var startTurn = actor.Turn;
    var hits = 0;
    var rangedTarget = new CommandTarget(true, true, CombatTeam.Enemies, CombatRow.Back);
    Check(!ranged.Confirm(system, "player", shoot, () => rangedTarget, () => { }));
    Check(ranged.ShotsRemaining(weapon) == 1);
    Check(ranged.Confirm(system, "player", shoot, () => rangedTarget, () => { }, hit =>
    {
        hits++;
        Check(hit.Damage == damage && hit.DamagePoints.Typed[DamageType.Piercing] == damage);
        Check(hit.Ammunition?.Ammunition == weaponTypeAmmo(type));
    }));
    Check(Math.Abs(actor.Turn - startTurn - attackTime) < 0.0001f);
    Check(hits == 1 && ranged.ShotsRemaining(weapon) == 0 && !ranged.Available(shoot));
    Check(!ranged.Confirm(system, "player", shoot, () => rangedTarget, () => { }, _ => hits++));
    // Equipment changes must not refill the weapon.
    Check(ranged.Commit(system, "player", new()));
    pendingRanged = ranged.BeginEquipment();
    Check(ranged.TryEquip(pendingRanged, weapon.Id.Value, slot));
    Check(ranged.Commit(system, "player", pendingRanged));
    Check(ranged.ShotsRemaining(weapon) == 0);
    if (!requiresBothHands)
    {
        ranged.Items.Add(new("blocker", "Occupied hand", otherSlot));
        pendingRanged = ranged.BeginEquipment();
        pendingRanged[otherSlot] = "blocker";
        Check(ranged.Commit(system, "player", pendingRanged));
        // Reload succeeds with the other hand occupied.
    }
    pendingRanged = ranged.BeginEquipment();
    startTurn = actor.Turn;
    Check(ranged.CanReload(pendingRanged, weapon.Id.Value, ammo.Id.Value));
    var queued = new Dictionary<string, string> { [weapon.Id.Value] = ammo.Id.Value };
    Check(Math.Abs(ranged.Cost(pendingRanged, queued) - reloadTime) < 0.0001f);
    Check(ranged.ShotsRemaining(weapon) == 0 && ranged.Items.Find(item => item.Id == ammo.Id)!.Quantity == 2);
    Check(ranged.Commit(system, "player", pendingRanged, queued));
    Check(Math.Abs(actor.Turn - startTurn - reloadTime) < 0.0001f);
    Check(hits == 1 && ranged.ShotsRemaining(weapon) == 1 && ranged.Available(shoot));
    Check(ranged.Items.Find(item => item.Id == ammo.Id)!.Quantity == 1);
    Check(!ranged.Commit(system, "player", pendingRanged, queued));
    Check(ranged.Confirm(system, "player", shoot, () => rangedTarget, () => { }, hit =>
    {
        Check(hit.Ammunition?.Id == ammo.Id && hit.Ammunition.Quantity == 1);
        Check(AttackImpact.ForStrike(hit).IconTexture == ammo.Icon);
    }));
    if (requiresBothHands)
    {
        pendingRanged = ranged.BeginEquipment();
        pendingRanged.Remove(otherSlot);
        Check(!ranged.Commit(system, "player", pendingRanged));
    }
}
Console.WriteLine("Ranged damage, timing, ammunition consumption, persistence, and reload with occupied hands checks passed.");
AmmunitionType weaponTypeAmmo(WeaponType type) => type switch
{
    WeaponType.Pistol => AmmunitionType.Bullet,
    WeaponType.Bow => AmmunitionType.Arrow,
    WeaponType.Crossbow => AmmunitionType.Bolt,
    WeaponType.Sling => AmmunitionType.Stone,
    _ => throw new ArgumentOutOfRangeException(nameof(type)),
};

// Preparing a specific throw is free and independent of targeting.
var prepared = CombatLoadout.CreatePlayerLoadout();
var preparedCombat = ThrowCombat();
var preparedTurn = preparedCombat.ActiveCombatant!.Turn;
Check(prepared.PrepareThrow("starting-sword") && prepared.CanThrowPreparedItem);
Check(prepared.CommandIcon(CombatLoadout.Throw) == prepared.Items.Single().Icon);
Check(preparedCombat.ActiveCombatant.Turn == preparedTurn && prepared.Items.Count == 1);
Check(!prepared.ConfirmPreparedThrow(preparedCombat, "player", () => null, _ => throw new Exception()));
Check(prepared.PreparedThrowItemId == "starting-sword" && prepared.Items.Count == 1);
var preparedHits = 0;
Check(prepared.ConfirmPreparedThrow(preparedCombat, "player", () => throwTarget, _ => preparedHits++));
Check(preparedHits == 1 && prepared.PreparedThrowItemId is null && prepared.Items.Count == 0);
Check(preparedCombat.ActiveCombatant!.Turn == preparedTurn + CombatLoadout.Throw.TurnCost);
Check(!prepared.ConfirmPreparedThrow(preparedCombat, "player", () => throwTarget, _ => preparedHits++));
Console.WriteLine("Prepared Throw is free until valid targeting and single-use confirmation.");

// Invalid or overcommitted ammunition cannot partially change equipment or spend Turn.
var reloadLoadout = new CombatLoadout();
var reloadCombat = ThrowCombat();
var firstPistol = WeaponCatalog.Create(WeaponType.Pistol, "pistol-a", "Pistol A", "Left hand");
var secondPistol = WeaponCatalog.Create(WeaponType.Pistol, "pistol-b", "Pistol B", "Right hand");
var oneBullet = WeaponCatalog.CreateAmmunition(AmmunitionType.Bullet, "one-bullet");
var wrongAmmo = WeaponCatalog.CreateAmmunition(AmmunitionType.Arrow, "arrow");
reloadLoadout.Items.AddRange([firstPistol, secondPistol, oneBullet, wrongAmmo]);
var reloadPending = reloadLoadout.BeginEquipment();
Check(reloadLoadout.TryEquip(reloadPending, firstPistol.Id.Value, "Left hand"));
Check(reloadLoadout.TryEquip(reloadPending, secondPistol.Id.Value, "Right hand"));
Check(reloadLoadout.Commit(reloadCombat, "player", reloadPending));
Check(reloadLoadout.Confirm(reloadCombat, "player", CombatLoadout.LeftHandStrike, () => throwTarget, () => { }, _ => { }));
Check(reloadLoadout.Confirm(reloadCombat, "player", CombatLoadout.RightHandStrike, () => throwTarget, () => { }, _ => { }));
Check(!reloadLoadout.CanReload(reloadPending, firstPistol.Id.Value, wrongAmmo.Id.Value));
var reloadStart = reloadCombat.ActiveCombatant!.Turn;
var excessiveReloads = new Dictionary<string, string> { ["pistol-a"] = "one-bullet", ["pistol-b"] = "one-bullet" };
Check(!reloadLoadout.Commit(reloadCombat, "player", reloadPending, excessiveReloads));
Check(reloadCombat.ActiveCombatant.Turn == reloadStart && reloadLoadout.ShotsRemaining(firstPistol) == 0
    && reloadLoadout.ShotsRemaining(secondPistol) == 0 && reloadLoadout.Items.Contains(oneBullet));
excessiveReloads.Remove("pistol-b");
Check(reloadLoadout.Commit(reloadCombat, "player", reloadPending, excessiveReloads));
Check(reloadLoadout.ShotsRemaining(firstPistol) == 1 && reloadLoadout.ShotsRemaining(secondPistol) == 0);
Check(!reloadLoadout.Items.Contains(oneBullet));
Console.WriteLine("Reload validates matching stacks atomically and tracks each equipped weapon independently.");

// Fixed inventory locations and shared movement rules for the Character dialog.
string Bag(int index) => CombatLoadout.InventorySlot(index);
bool SameLayout(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b) =>
    a.Count == b.Count && a.All(pair => b.GetValueOrDefault(pair.Key) == pair.Value);
var locations = CombatLoadout.CreatePlayerLoadout();
var locationCombat = ThrowCombat();
Check(locations.Inventory.Count == 20 && locations.Inventory.All(item => item is null));
Check(CombatLoadout.EquipmentSlots.Count == 9);
var layout = locations.BeginItemLayout();
Check(locations.TryMoveItem(layout, "Right hand", "Left hand"));
Check(layout.GetValueOrDefault("Left hand") == "starting-sword" && !layout.ContainsKey("Right hand"));
Check(Math.Abs(locations.ItemLayoutCost(layout) - 1.2f) < 0.0001f);
Check(locations.Equipment.ContainsKey("Right hand")); // Preview does not mutate committed equipment.
Check(locations.TryMoveItem(layout, "Left hand", "Right hand"));
Check(locations.ItemLayoutCost(layout) == 0f);
Check(locations.TryMoveItem(layout, "Right hand", Bag(19)));
Check(layout[Bag(19)] == "starting-sword" && !layout.ContainsKey("Right hand"));
var locationStart = locationCombat.ActiveCombatant!.Turn;
Check(locations.CommitItemLayout(locationCombat, "player", layout));
Check(Math.Abs(locationCombat.ActiveCombatant.Turn - locationStart - 1.2f) < 0.0001f);
Check(locations.Inventory[19]?.Id.Value == "starting-sword" && locations.Inventory.Take(19).All(item => item is null));
layout = locations.BeginItemLayout();
Check(locations.TryMoveItem(layout, Bag(19), "Left hand"));
Check(locations.CommitItemLayout(locationCombat, "player", layout));
Check(locations.Inventory.All(item => item is null) && locations.Equipment["Left hand"] == "starting-sword");
Check(locations.TryAddItem(WeaponCatalog.Create(WeaponType.Knives, "knife", "Knife", "Right hand")));
Check(locations.TryAddItem(new("hat-layout", "Hat", "Head")));
Check(locations.TryAddItem(WeaponCatalog.CreateAmmunition(AmmunitionType.Bullet, "stack-layout", 7)));
layout = locations.BeginItemLayout();
Check(locations.TryMoveItem(layout, Bag(0), "Right hand"));
Check(locations.TryMoveItem(layout, "Left hand", "Right hand"));
Check(layout["Left hand"] == "knife" && layout["Right hand"] == "starting-sword");
Check(locations.TryMoveItem(layout, "Right hand", Bag(5)));
Check(locations.TryMoveItem(layout, Bag(5), Bag(2))); // Sword and ammunition stack exchange cells.
Check(layout[Bag(5)] == "stack-layout" && layout[Bag(2)] == "starting-sword");
Check(locations.Items.Find(item => item.Id.Value == "stack-layout")!.Quantity == 7);
var beforeBadMove = new Dictionary<string, string>(layout);
Check(!locations.TryMoveItem(layout, Bag(1), "Left hand")); // Hat cannot equip in a hand.
Check(!locations.TryMoveItem(layout, "Left hand", Bag(1))); // Swap cannot put a hat in the source hand.
Check(!locations.TryMoveItem(layout, Bag(2), "Inventory:20"));
Check(SameLayout(layout, beforeBadMove));
var cancelledLayout = locations.BeginItemLayout();
Check(cancelledLayout["Left hand"] == "starting-sword" && cancelledLayout[Bag(0)] == "knife");
Check(locations.CommitItemLayout(locationCombat, "player", layout));
Check(locations.Inventory[5]?.Quantity == 7 && locations.Inventory[2]?.Id.Value == "starting-sword");
Check(locations.Inventory[0] is null); // Empty cells do not collapse.
var duplicateLayout = locations.BeginItemLayout();
duplicateLayout[Bag(18)] = "starting-sword";
locationStart = locationCombat.ActiveCombatant!.Turn;
Check(!locations.CommitItemLayout(locationCombat, "player", duplicateLayout));
Check(locationCombat.ActiveCombatant.Turn == locationStart);
Console.WriteLine("Twenty inventory cells: hand transfers, unequipping, swaps, stack relocation, cancellation, costs and invalid drops passed.");

// Full inventory must neither lose displaced items nor silently create a twenty-first cell.
var crowded = new CombatLoadout();
Check(crowded.TryAddItem(WeaponCatalog.Create(WeaponType.Bow, "crowded-bow", "Bow", "Right hand")));
for (var index = 1; index < 20; index++)
    Check(crowded.TryAddItem(new($"filler-{index}", "Filler", "Inventory")));
Check(!crowded.TryAddItem(new("overflow", "Overflow", "Inventory")));
crowded.Items.Add(WeaponCatalog.Create(WeaponType.Pistol, "crowded-left", "Pistol", "Left hand"));
crowded.Items.Add(WeaponCatalog.Create(WeaponType.Pistol, "crowded-right", "Pistol", "Right hand"));
var crowdedCombat = ThrowCombat();
var crowdedEquipment = new Dictionary<string, string> { ["Left hand"] = "crowded-left", ["Right hand"] = "crowded-right" };
Check(crowded.Commit(crowdedCombat, "player", crowdedEquipment));
var crowdedLayout = crowded.BeginItemLayout();
var beforeCrowded = new Dictionary<string, string>(crowdedLayout);
Check(!crowded.TryMoveItem(crowdedLayout, Bag(0), "Right hand"));
Check(SameLayout(crowdedLayout, beforeCrowded) && crowded.Inventory.Count == 20);
Check(!crowded.TryMoveItem(crowdedLayout, "Left hand", Bag(1))); // Incompatible displaced filler.
crowded.Items.RemoveAll(item => item.Id.Value == "filler-19");
crowdedLayout = crowded.BeginItemLayout();
Check(crowded.TryMoveItem(crowdedLayout, Bag(0), "Right hand"));
Check(crowdedLayout["Left hand"] == "crowded-bow" && crowdedLayout["Right hand"] == "crowded-bow");
Check(crowdedLayout[Bag(0)] == "crowded-left" && crowdedLayout[Bag(19)] == "crowded-right");
Check(crowded.CommitItemLayout(crowdedCombat, "player", crowdedLayout));
Check(crowded.TryMoveItem(crowdedLayout, "Left hand", Bag(0)));
Check(crowdedLayout[Bag(0)] == "crowded-bow" && crowdedLayout["Left hand"] == "crowded-left" && !crowdedLayout.ContainsKey("Right hand"));
Check(crowded.CommitItemLayout(crowdedCombat, "player", crowdedLayout));
Check(crowded.Items.Count == 21 && crowded.Inventory.Count(item => item is not null) == 20);
Console.WriteLine("Full inventory, two-handed displacement, capacity failure and atomic exchanges passed.");

// A reload consumes its located stack without moving neighboring inventory items.
var locatedAmmo = new CombatLoadout();
Check(locatedAmmo.TryAddItem(WeaponCatalog.Create(WeaponType.Pistol, "located-pistol", "Pistol", "Right hand")));
Check(locatedAmmo.TryAddItem(WeaponCatalog.CreateAmmunition(AmmunitionType.Bullet, "located-bullet")));
Check(locatedAmmo.TryAddItem(new("neighbor", "Neighbor", "Inventory")));
var locatedAmmoCombat = ThrowCombat();
var locatedAmmoLayout = locatedAmmo.BeginItemLayout();
Check(locatedAmmo.TryMoveItem(locatedAmmoLayout, Bag(0), "Left hand"));
Check(locatedAmmo.CommitItemLayout(locatedAmmoCombat, "player", locatedAmmoLayout));
Check(locatedAmmo.Confirm(locatedAmmoCombat, "player", CombatLoadout.LeftHandStrike, () => throwTarget, () => { }, _ => { }));
locatedAmmoLayout = locatedAmmo.BeginItemLayout();
Check(locatedAmmo.CommitItemLayout(locatedAmmoCombat, "player", locatedAmmoLayout,
    new Dictionary<string, string> { ["located-pistol"] = "located-bullet" }));
Check(locatedAmmo.Inventory[1] is null && locatedAmmo.Inventory[2]?.Id.Value == "neighbor");
Check(locatedAmmo.ShotsRemaining(locatedAmmo.Items.Find(item => item.Id.Value == "located-pistol")!) == 1);
Console.WriteLine("Reload preserves inventory cell positions and consumes ammunition only on commit.");

// Throwing one unit of an equipped stack retains the remainder in its equipment cell.
var equippedStackLoadout = new CombatLoadout();
var equippedStack = WeaponCatalog.Create(WeaponType.Knives, "equipped-stack", "Knives", "Right hand") with { Quantity = 3 };
Check(equippedStackLoadout.TryAddItem(equippedStack));
var equippedStackCombat = ThrowCombat();
var equippedStackLayout = equippedStackLoadout.BeginItemLayout();
Check(equippedStackLoadout.TryMoveItem(equippedStackLayout, Bag(0), "Left hand"));
Check(equippedStackLoadout.CommitItemLayout(equippedStackCombat, "player", equippedStackLayout));
Check(equippedStackLoadout.PrepareThrow("equipped-stack"));
Check(equippedStackLoadout.ConfirmPreparedThrow(equippedStackCombat, "player", () => throwTarget, _ => { }));
Check(equippedStackLoadout.Equipment["Left hand"] == "equipped-stack");
Check(equippedStackLoadout.HeldItem(StrikeHand.Left)!.Quantity == 2);
Check(equippedStackLoadout.Inventory.All(item => item is null));
Console.WriteLine("Throwing an equipped stack keeps its remainder in the same location.");

// A prepared inventory stack remains selected until that exact stack is depleted.
var persistentThrow = new CombatLoadout();
Check(persistentThrow.TryAddItem(WeaponCatalog.Create(WeaponType.Knives, "throw-stack-a", "Knives A", "Right hand") with { Quantity = 2 }));
Check(persistentThrow.TryAddItem(WeaponCatalog.Create(WeaponType.Knives, "throw-stack-b", "Knives B", "Right hand") with { Quantity = 3 }));
var persistentCombat = ThrowCombat();
persistentThrow.BeginItemLayout();
Check(persistentThrow.PrepareThrow("throw-stack-a"));
Check(persistentThrow.ConfirmPreparedThrow(persistentCombat, "player", () => throwTarget, _ => { }));
Check(persistentThrow.PreparedThrowItemId == "throw-stack-a" && persistentThrow.Inventory[0]?.Quantity == 1);
Check(persistentThrow.ConfirmPreparedThrow(persistentCombat, "player", () => throwTarget, _ => { }));
Check(persistentThrow.PreparedThrowItemId is null && persistentThrow.Inventory[0] is null);
Check(persistentThrow.Inventory[1]?.Id.Value == "throw-stack-b" && persistentThrow.Inventory[1]?.Quantity == 3);
Check(!persistentThrow.ConfirmPreparedThrow(persistentCombat, "player", () => throwTarget, _ => throw new Exception()));
Console.WriteLine("Prepared Throw persists for the source stack and clears on exhaustion without selecting another stack.");

// Any damage source uses the same receiver and reports the post-mitigation profile.
var damageReceiver = new DamageReceiver(5);
var damageEvents = new List<DamageTaken>();
var healthNotifications = 0;
damageReceiver.HealthChanged += () => healthNotifications++;
damageReceiver.DamageTaken += hit =>
{
    Check(damageReceiver.Health == hit.HealthAfter);
    damageEvents.Add(hit);
};
var mixedHit = new DamageProfile(new Dictionary<DamageType, int>
{
    [DamageType.Slashing] = 2, [DamageType.Fire] = 2,
});
var mitigatedHit = DamageResolver.Apply(damageReceiver, mixedHit, hit => hit.Reduce(DamageType.Slashing, 1));
Check(damageReceiver.Health == 2 && mitigatedHit.HealthLost == 3 && !mitigatedHit.IsLethal);
Check(damageEvents.Count == 1 && damageEvents[0].Damage.Typed[DamageType.Slashing] == 1);
Check(damageEvents[0].Damage.Typed[DamageType.Fire] == 2 && mixedHit.Total == 4);
DamageResolver.Apply(damageReceiver, mixedHit, _ => new DamageProfile());
DamageResolver.Apply(damageReceiver, new DamageProfile());
Check(damageReceiver.Health == 2 && damageEvents.Count == 1 && healthNotifications == 1);
var lethalHit = DamageResolver.Apply(damageReceiver, mixedHit);
Check(lethalHit.IsLethal && lethalHit.HealthLost == 2 && lethalHit.Damage.Total == 4);
Check(damageReceiver.Health == 0 && damageEvents.Count == 2 && healthNotifications == 2);
DamageResolver.Apply(damageReceiver, mixedHit);
Check(damageEvents.Count == 2 && healthNotifications == 2);
damageReceiver.Health = 5;
Check(damageEvents.Count == 2 && healthNotifications == 3);
Console.WriteLine("Shared damage resolution, mitigation, notifications, overkill and defeated targets passed.");

foreach (var (type, effect) in new[]
{
    (WeaponType.Sword, "slash_01"), (WeaponType.Axe, "slash_02"), (WeaponType.Knives, "slash_03"),
    (WeaponType.Clubs, "impact_02"), (WeaponType.Maces, "impact_03"), (WeaponType.Flail, "impact_04"),
    (WeaponType.Pistol, "ranged_01"), (WeaponType.Bow, "arrow_01"),
    (WeaponType.Crossbow, "arrow_02"), (WeaponType.Sling, "ranged_02"),
})
{
    var item = WeaponCatalog.Create(type, "overlay-test", "Test", "Right hand");
    Check(AttackImpact.ForStrike(new(StrikeHand.Right, item)).EffectTexture == $"effects.{effect}.png");
    Check(AttackImpact.ForThrow(item).IconTexture == item.Icon);
}
Check(AttackImpact.ForStrike(new(StrikeHand.Left, null)).EffectTexture == "effects.impact_01.png");
var customEffectWeapon = WeaponCatalog.Create(WeaponType.Pistol, "custom-effect", "Custom", "Right hand");
customEffectWeapon = customEffectWeapon with { Weapon = customEffectWeapon.Weapon! with { ImpactTexture = "effects.ranged_04.png" } };
Check(AttackImpact.ForStrike(new(StrikeHand.Right, customEffectWeapon)).EffectTexture == "effects.ranged_04.png");
var impactReceiver = new DamageReceiver(5);
var impacts = 0;
var impactDamageEvents = 0;
var attackImpact = new AttackImpact("effects.slash.png");
impactReceiver.ImpactOccurred += impact =>
{
    Check(impact == AttackImpact.Blocked && impact.IconTexture is null);
    impacts++;
};
impactReceiver.DamageTaken += _ => impactDamageEvents++;
DamageResolver.Apply(impactReceiver, mixedHit, _ => new DamageProfile(), attackImpact);
Check(impacts == 1 && impactDamageEvents == 0 && impactReceiver.Health == 5);
DamageResolver.Apply(impactReceiver, new DamageProfile(), impact: new AttackImpact(IconTexture: "thrown-icon"));
Check(impacts == 2 && impactDamageEvents == 0 && impactReceiver.Health == 5);
DamageResolver.Apply(impactReceiver, mixedHit);
Check(impacts == 2 && impactDamageEvents == 1);
impactReceiver.Health = 0;
DamageResolver.Apply(impactReceiver, mixedHit, impact: attackImpact);
Check(impacts == 2 && impactDamageEvents == 1);
Console.WriteLine("Attack artwork mapping and impact feedback independent of damage passed.");

// Presentation owns automatic turns until it explicitly completes them.
var presentedCombat = new CombatSystem();
presentedCombat.Add(new Combatant("enemy", false, team: CombatTeam.Enemies));
presentedCombat.Add(new Combatant("player", true, 0.2f));
presentedCombat.Process();
Check(presentedCombat.ActiveCombatant?.Id == "enemy");
var automaticResolutions = 0;
presentedCombat.Process();
Check(presentedCombat.ActiveCombatant?.Id == "enemy");
presentedCombat.SubmitAutomaticAction(new(0.6f, (_, actor) =>
{
    Check(actor.Id == "enemy");
    automaticResolutions++;
}));
Check(automaticResolutions == 1 && presentedCombat.ActiveCombatant?.Id == "player");
Check(presentedCombat.Timeline.TurnOrder.Single(entry => entry.CombatantId == "enemy").Turn == 0.6f);
try
{
    presentedCombat.SubmitAutomaticAction(new(1f, (_, _) => automaticResolutions++));
    throw new Exception("Automatic submission accepted a player turn.");
}
catch (InvalidOperationException) { }
Check(automaticResolutions == 1);
Console.WriteLine("Presented automatic turns yield, resolve once, and preserve timeline costs.");
