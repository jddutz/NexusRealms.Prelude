using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;

static void Check(bool condition) { if (!condition) throw new Exception("Story outcome check failed"); }
var story = new Storyline();
var scenario = (CombatScenario)story.Nodes[story.StartNodeId];
Check(scenario.VictoryConditions.Length == 0 && scenario.DefeatConditions.Length == 1);
Check(scenario.Characters.Count(character => character.RequiredForVictory) == 1);
Check(scenario.Characters.Single(character => character.RequiredForVictory).Slot == NexusRealms.Prelude.FormationSlot.BackLeft);
var encounter = new CombatSystem();
encounter.Add(new("player", true));
for (var index = 0; index < scenario.Characters.Length; index++)
    encounter.Add(new($"encounter-{index}", false, turn: 2f, team: CombatTeam.Enemies));
encounter.VictoryConditions = scenario.BuildVictoryConditions();
Check(encounter.VictoryConditions.Length == 1);
encounter.Process();
var requiredIndex = Enumerable.Range(0, scenario.Characters.Length).Single(index => scenario.Characters[index].RequiredForVictory);
encounter.SubmitAction(new(1f, (system, _) => system.Remove($"encounter-{requiredIndex}")));
Check(encounter.Outcome == CombatOutcome.Victory && encounter.HasLivingEnemies);
var routes = new[] { scenario.VictoryNode, scenario.DefeatNode, scenario.EscapeNode };
Check(routes.All(route => route is not null));
Check(routes.Distinct().Count() == 3);
foreach (var route in routes) Check(story.Nodes[route!.Value] is OutcomeNode);
var state = new GameState(story.StartNodeId);
state.LastCombatOutcome = CombatOutcome.Victory;
state.CurrentStoryNodeId = scenario.VictoryNode!.Value;
Check(((OutcomeNode)story.Nodes[state.CurrentStoryNodeId]).Title == "Victory");
Check(state.LastCombatOutcome == CombatOutcome.Victory);
Console.WriteLine("Story outcome route checks passed.");
