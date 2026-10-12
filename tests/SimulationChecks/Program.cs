using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;
using NexusRealms.Prelude.Simulator;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var storyline = new Storyline();
var state = new SimulationState { CurrentStoryNodeId = storyline.StartNodeId.Value };
var startingLoadout = state.CreateGameState().Loadout;
Check(startingLoadout.Inventory.Single(item => item?.Id.Value == "starting-rocks")?.Quantity == 5,
    "Player must start with one inventory stack of five rocks.");
var first = SimulationRunner.Run(storyline, state, 42);
Check(first == SimulationRunner.Run(storyline, state, 42), "Seed replay or fresh-state isolation failed.");
Check(state.Health == 20, "Simulation mutated its starting state.");
Check(SimulationRunner.Run(storyline, state with { Health = 0 }, 42).Outcome == CombatOutcome.Defeat, "Dead player must lose immediately.");
var combat = new CombatSystem { TurnLimit = 100 };
var resolved = false;
combat.ScheduleEvent(100, "cutoff", _ => resolved = true);
combat.Process();
Check(combat.Outcome == CombatOutcome.Draw && !resolved, "An event at Turn 100 must not resolve.");
var before = new CombatSystem { TurnLimit = 100 };
before.ScheduleEvent(99.99f, "victory", system => system.End(CombatOutcome.Victory));
before.Process();
Check(before.Outcome == CombatOutcome.Victory, "Outcome before cutoff must count.");
var pump = new CombatSystem { TurnLimit = 100 };
pump.Add(new("player", true, 99));
pump.Add(new("enemy", false, 100));
pump.Process();
pump.SubmitAction(new(1, (_, _) => { }));
Check(pump.Outcome == CombatOutcome.Draw, "Automatic processing after an action crossed cutoff.");
var empty = (state with { Items = [], ItemLayout = [] }).CreateGameState();
Check(empty.Loadout.Items.Count == 0 && empty.Loadout.Equipment.Count == 0, "Empty saved loadout was not restored.");
var authored = (CombatScenario)storyline.Nodes[storyline.StartNodeId];
var harmless = storyline.Characters.ToDictionary(pair => pair.Key, pair => pair.Value with
{
    Health = int.MaxValue,
    Attacks = [new() { Damage = new DamageProfile() }],
});
var draw = SimulationRunner.Run(authored, harmless, state.CreateGameState(), 42);
Check(draw.Outcome == CombatOutcome.Draw && draw.Turn == 100, "Full encounter did not draw at Turn 100.");
var selectedRockThrow = false;
for (var seed = 0; seed < 32 && !selectedRockThrow; seed++)
{
    var throwState = state.CreateGameState();
    SimulationRunner.Run(authored, harmless, throwState, seed);
    selectedRockThrow = throwState.Loadout.ThrownItems.Any(item => item.Id.Value == "starting-rocks");
}
Check(selectedRockThrow, "Simulator random action selection must include throwing inventory rocks.");
try
{
    SimulationRunner.Run(storyline, state with { CurrentStoryNodeId = "missing" }, 42);
    throw new Exception("Missing scenario was accepted.");
}
catch (ArgumentException) { }
Console.WriteLine("Simulation checks passed.");
