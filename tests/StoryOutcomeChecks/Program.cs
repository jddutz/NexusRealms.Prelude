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

// Spent HUD segments retain their positions, brighten, fade, and recover independently.
var barTexture = new Nexus.Graphics.Textures.Texture(new("bar-test"), 1, 1,
    [new Nexus.Graphics.Color(0.5f, 0.1f, 0.1f, 0.75f)]);
var resourceRow = new NexusRealms.Prelude.ResourceRow(barTexture, animateLoss: true);
resourceRow.SetPoints(5, 14f, -2f);
Check(!resourceRow.IsShowingLoss && resourceRow.Children.Count == 5);
resourceRow.SetPoints(3, 14f, -2f);
Check(resourceRow.IsShowingLoss && resourceRow.Children.Count == 5);
var spentSegment = (Nexus.GUI.Elements.ImageElement)resourceRow.Children[4];
Check(spentSegment.Texture != barTexture && spentSegment.Color.A == 1f);
var flashPixels = new byte[4];
spentSegment.Texture!.WriteTo(0, 1, Nexus.Graphics.ColorFormatEnum.RGBA8UNorm, flashPixels);
Check(flashPixels[0] > 225 && flashPixels[0] == flashPixels[1] && flashPixels[1] == flashPixels[2]);
Check(Math.Abs(flashPixels[3] - 191) <= 1);
resourceRow.Update(0.08d);
Check(spentSegment.Color.A == 1f);
resourceRow.Update(0.16d);
Check(Math.Abs(spentSegment.Color.A - 0.5f) < 0.001f);
resourceRow.SetPoints(2, 14f, -2f);
Check(((Nexus.GUI.Elements.ImageElement)resourceRow.Children[2]).Color.A == 1f);
resourceRow.Update(0.16d);
Check(resourceRow.Children.Count == 3 && resourceRow.IsShowingLoss);
resourceRow.SetPoints(3, 14f, -2f);
Check(!resourceRow.IsShowingLoss && resourceRow.Children.Count == 3);
Check(((Nexus.GUI.Elements.ImageElement)resourceRow.Children[2]).Texture == barTexture);
resourceRow.AnimationSpeed = 2d;
resourceRow.SetPoints(0, 14f, -2f);
resourceRow.Update(0.2d);
Check(!resourceRow.IsShowingLoss && resourceRow.Children.Count == 0);
Console.WriteLine("Player resource flash, fade, repeated loss, recovery, and game speed checks passed.");
