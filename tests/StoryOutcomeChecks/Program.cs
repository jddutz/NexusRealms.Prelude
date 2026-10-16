using NexusRealms.Prelude.Rules;
using NexusRealms.Prelude;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;

static void Check(bool condition) { if (!condition) throw new Exception("Story outcome check failed"); }
var story = new Storyline();
var scenario = (CombatScenario)story.Nodes[NexusRealms.Prelude.Database.Chapter1.Part1.Intro.Id];
Check(scenario.VictoryConditions.Length == 0 && scenario.DefeatConditions.Length == 1);
Check(scenario.Characters.Count(character => character.RequiredForVictory) == 1);
Check(scenario.Characters.Single(character => character.RequiredForVictory).Slot == NexusRealms.Prelude.DataModel.FormationSlot.BackLeft);
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

var actionRandom = new Random(1);
var clubState = new NexusRealms.Prelude.EnemyActionState(NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug1);
var clubAction = clubState.Select(actionRandom);
Check(clubAction.TurnCost == WeaponCatalog.Get(WeaponType.Sword).Weapon!.AttackSpeed);
Check(clubAction.TurnCost == 1f && clubAction.Damage.Total == 3);
var pistolState = new NexusRealms.Prelude.EnemyActionState(NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug2);
var pistol = pistolState.Items.Single(item => item.Weapon is not null);
var shot = pistolState.Select(actionRandom);
Check(!shot.Reload && shot.TurnCost == 0.1f && shot.Damage.Total == 3 && pistolState.ShotsRemaining(pistol) == 1);
pistolState.Apply(shot);
Check(pistolState.ShotsRemaining(pistol) == 0);
var reload = pistolState.Select(actionRandom);
Check(reload.Animation == "Reload" && reload.Reload && reload.TurnCost == 1.4f && reload.Damage.Total == 0 && reload.ImpactFraction == 1f);
Check(pistolState.ShotsRemaining(pistol) == 0); // Selection alone never loads ammunition.
pistolState.Apply(reload);
Check(pistolState.ShotsRemaining(pistol) == 1);
Check(pistolState.Items.Single(item => item.Ammunition == AmmunitionType.Bullet).Quantity == 5);
Check(!pistolState.Select(actionRandom).Reload);
var rocksState = new NexusRealms.Prelude.EnemyActionState(NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug3);
for (var remainingRocks = 3; remainingRocks > 0; remainingRocks--)
{
    Check(rocksState.Items.Single().Quantity == remainingRocks);
    var throwRock = rocksState.Select(actionRandom, action => action.Throw);
    Check(throwRock.Throw && throwRock.Animation == "Attack" && throwRock.TurnCost == 1f && throwRock.Damage.Total == 2);
    rocksState.Apply(throwRock);
}
Check(rocksState.Items.Count == 0 && rocksState.Select(actionRandom, action => action.Throw).Damage.Total == 0);
Check(NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug3.Items.Single().Quantity == 3);
var rock = WeaponCatalog.Create(WeaponType.Rock, "test-rock", "Rock", "Right hand");
var rockLoadout = new CombatLoadout();
Check(rockLoadout.TryAddItem(rock) && rockLoadout.CanEquip(rock, "Right hand") && rockLoadout.CanEquip(rock, "Left hand"));
Check(rock.Icon == "items.rock.png" && rock.ThrowDamage!.Typed[DamageType.Crushing] == 2);

// Replay real enemy costs against a sword turn; ignore health so every actor gets a turn.
var costCombat = new CombatSystem();
costCombat.Add(new("player", true));
costCombat.Add(new("club", false, team: CombatTeam.Enemies));
costCombat.Add(new("pistol", false, team: CombatTeam.Enemies));
costCombat.Add(new("rock", false, team: CombatTeam.Enemies));
costCombat.Process();
costCombat.SubmitAction(new(1f, (_, _) => { }));
var costPistolState = new NexusRealms.Prelude.EnemyActionState(NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug2);
var enemyTurns = new List<(string Id, float Turn)>();
while (costCombat.ActiveCombatant is { PlayerControlled: false } costActor)
{
    enemyTurns.Add((costActor.Id, costActor.Turn));
    var costAction = costActor.Id == "pistol" ? costPistolState.Select(actionRandom) : clubAction;
    if (costActor.Id == "pistol") costPistolState.Apply(costAction);
    costCombat.SubmitAutomaticAction(new(costAction.TurnCost, (_, _) => { }));
}
Check(costCombat.ActiveCombatant?.Id == "player" && costCombat.Timeline.CurrentTurn == 1f);
Check(enemyTurns.Count(entry => entry.Id == "club") == 1 && enemyTurns.Count(entry => entry.Id == "rock") == 1);
Check(enemyTurns.Where(entry => entry.Id == "pistol").Select(entry => entry.Turn).SequenceEqual(new[] { 0f, 0.1f }));
Check(costCombat.Timeline.TurnOrder.Single(entry => entry.CombatantId == "pistol").Turn == 1.5f);
Console.WriteLine("Enemy weapon costs, typed damage, pistol reload, three rocks, equipment, and second-player-turn ordering passed.");

// Validate the authored action names against the exported animation sheets when available.
var contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.content"));
foreach (var thug in new[]
{
    NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug1,
    NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug2,
    NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug3,
})
{
    var sheetPath = Path.Combine(contentRoot, "Textures/Characters", thug.Artwork.Value["characters.".Length..].Replace(".png", ".json"));
    var sheet = Nexus.Graphics.AsepriteSpriteSheet.Load(sheetPath);
    foreach (var action in thug.Actions)
    {
        var animation = sheet.GetAnimation(action.Animation);
        Check(!animation.Loop && animation.Frames.Count > 1 && animation.Duration.TotalSeconds > 0);
        var reloadAnimation = sheet.GetAnimation(action.ReloadAnimation);
        Check(!reloadAnimation.Loop && reloadAnimation.Frames.Count > 1);
    }
}
Console.WriteLine("All three thug attack names resolve to exported multi-frame animations.");
