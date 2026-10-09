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
commandsCombat.Add(new Combatant("player", true));
commandsCombat.Process();
var loadout = new CombatLoadout { Focus = 2 };
var commandExecutions = 0;
var slash = new CommandDefinition("slash", "Slash", 0.6f, "sword", "Right hand", FocusCost: 1,
    Resolve: (_, _) => commandExecutions++);
loadout.Inventory.Add(new("sword", "Sword", "Right hand", 2));
loadout.Abilities.Add(slash);
loadout.Abilities.Add(new("locked", "Locked", 1f, Learned: false));
loadout.Abilities.Add(new("passive", "Passive", 1f, Passive: true));
Check(loadout.Assign(0, "slash") && commandExecutions == 0);
Check(!loadout.Assign(0, "locked") && !loadout.Assign(0, "passive"));
Check(!loadout.Available(slash));
var pendingLoadout = loadout.BeginEquipment();
pendingLoadout["Right hand"] = "sword";
Check(loadout.Equipment.Count == 0 && loadout.Cost(pendingLoadout) == 0.6f);
Check(loadout.InitiativeBonus(pendingLoadout) == 2);
pendingLoadout.Clear();
Check(loadout.Cost(pendingLoadout) == 0f);
pendingLoadout["Right hand"] = "missing";
var beforeCommit = commandsCombat.Timeline.CurrentTurn;
Check(!loadout.Commit(commandsCombat, "player", pendingLoadout));
Check(loadout.Equipment.Count == 0 && commandsCombat.Timeline.CurrentTurn == beforeCommit);
pendingLoadout["Right hand"] = "sword";
Check(loadout.Commit(commandsCombat, "player", pendingLoadout));
Check(loadout.Equipment["Right hand"] == "sword" && loadout.Available(slash));
Check(commandsCombat.ActiveCombatant!.Initiative == 2);
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

Check(commandsCombat.ActiveCombatant!.Initiative == 0);
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
