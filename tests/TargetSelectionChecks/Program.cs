global using Nexus.Core;
global using Nexus.Game;
global using Nexus.Graphics;
global using Nexus.Graphics.Components;
global using Nexus.Graphics.Textures;
global using Silk.NET.Maths;
using NexusRealms.Prelude;
using NexusRealms.Prelude.Actions;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

static void Check(bool value) { if (!value) throw new Exception("Check failed"); }
var texture = new Texture(new("test"), 2, 2,
    [new(1f, 1f, 1f, 0f), new(1f, 1f, 1f, 1f), new(1f, 1f, 1f, 1f), new(1f, 1f, 1f, 1f)]);
var valid = new Texture(new("valid"), 1, 1, [new(0f, 1f, 0f, 1f)]);
var invalid = new Texture(new("invalid"), 1, 1, [new(1f, 0f, 0f, 1f)]);
var character = new Character(new CharacterData
    { Id = "test", Name = "Test", Artwork = new("test"), Portrait = new("test") }, texture, texture);
character.ConfigureFocusIndicator(valid, invalid, 1);
ITexture? firstPublishedTexture = null;
character.FocusRenderer.DrawableAdded += (_, _) =>
    firstPublishedTexture ??= character.FocusRenderer.Texture;
var changes = 0;
character.FocusChanged += _ => changes++;
character.SetFocus(true, true);
Check(character.IsFocused && character.FocusRenderer.IsVisible && character.FocusRenderer.Texture == valid);
Check(firstPublishedTexture == valid);
character.SetFocus(true, true);
Check(changes == 1);
character.SetFocus(true, false);
Check(character.FocusRenderer.Texture == invalid && changes == 1);
character.SetFocus(false, false);
Check(!character.FocusRenderer.IsVisible && changes == 2);
character.Position = new(10f, 20f);
character.Scale = new(2f, 2f);
Check(!character.HitTest(new(9f, 17f))); // Transparent top-left pixel.
Check(character.HitTest(new(11f, 17f))); // Opaque top-right pixel.
Check(!character.HitTest(new(15f, 17f)));
character.Elevation = 3f;
Check(character.HitTest(new(11f, 11f)));
Check(character.Health == 5 && character.Focus == 2);
var resourcesChanged = 0;
character.ResourcesChanged += _ => resourcesChanged++;
character.Health = 3;
character.Focus = 1;
character.Focus = 1;
Check(resourcesChanged == 2 && character.Health == 3 && character.Focus == 1);
try { character.Health = -1; throw new Exception("Accepted negative health"); }
catch (ArgumentOutOfRangeException) { }
var formation = new CharacterFormation(new(1000f, 1000f));
formation.SetSlot(FormationSlot.FrontCenter, character);
Check(character.Scale.Y == 1000f * 0.62f * 0.75f / texture.Height);
formation.SetVerticalLimits(200f, 600f);
Check(character.Scale.Y == 400f * 0.75f / texture.Height);
character.SetIndicatorScreenScale(2f, 2f);
var screenHeight = character.FocusRenderer.Instances.Values.Single().Size.Y * character.Scale.Y / 2f;
Check(MathF.Abs(screenHeight - 48f) < 0.001f);
character.Scale = new(0.5f, 0.5f);
character.SetIndicatorScreenScale(3f, 3f);
screenHeight = character.FocusRenderer.Instances.Values.Single().Size.Y * character.Scale.Y / 3f;
Check(MathF.Abs(screenHeight - 48f) < 0.001f);
Console.WriteLine("Target selection checks passed.");




// The same character can receive different initiative in different scenarios.
var firstPlacement = new CharacterPlacement { CharacterId = "test", Slot = FormationSlot.FrontCenter, Initiative = 1 };
var secondPlacement = firstPlacement with { Initiative = 7 };
Check(firstPlacement.CharacterId == secondPlacement.CharacterId && firstPlacement.Initiative == 1);
Check(new CharacterPlacement { CharacterId = "test", Slot = FormationSlot.FrontCenter }.Initiative == 0);
var encounter = new NexusRealms.Prelude.Combat.CombatSystem();
encounter.Add(new("first-scenario", true, initiative: firstPlacement.Initiative));
encounter.Add(new("second-scenario", true, initiative: secondPlacement.Initiative));
encounter.Process();
Check(encounter.ActiveCombatant!.Id == "second-scenario");
encounter.SubmitAction(new(0.6f, (_, _) => { }));
Check(encounter.ActiveCombatant!.Id == "first-scenario");
Console.WriteLine("Scenario placement initiative checks passed.");

// Overlays and damage tints finish independently, including on a lethal hit.
var feedbackCharacter = new Character(new CharacterData
    { Id = "feedback", Name = "Feedback", Artwork = new("test"), Portrait = new("test") }, texture, texture);
var feedbackTextures = new FeedbackTextures(texture);
feedbackCharacter.ConfigureEffectOverlays(feedbackTextures, 1);
NexusRealms.Prelude.Combat.DamageResolver.Apply(feedbackCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.Basic] = 5 }),
    impact: new("effects.ranged.png", "test-ammo"));
Check(feedbackTextures.Requests.Select(id => id.Value).SequenceEqual(new[] { "effects.ranged.png", "test-ammo" }));
Check(feedbackCharacter.IsShowingFeedback && feedbackCharacter.Renderer.IsVisible);
feedbackCharacter.Update(0.25d);
Check(feedbackCharacter.IsShowingDamage && feedbackCharacter.Renderer.IsVisible);
feedbackCharacter.Update(0.25d);
Check(feedbackCharacter.IsShowingFeedback && feedbackCharacter.Renderer.IsVisible);
feedbackCharacter.Update(0.25d);
Check(!feedbackCharacter.IsShowingFeedback && !feedbackCharacter.Renderer.IsVisible && !feedbackCharacter.ShadowRenderer.IsVisible);
var blockedCharacter = new Character(new CharacterData
    { Id = "blocked", Name = "Blocked", Artwork = new("test"), Portrait = new("test") }, texture, texture);
blockedCharacter.ConfigureEffectOverlays(feedbackTextures, 1);
NexusRealms.Prelude.Combat.DamageResolver.Apply(blockedCharacter.DamageReceiver, new(), impact: new("effects.impact.png"));
Check(blockedCharacter.IsShowingFeedback && !blockedCharacter.IsShowingDamage);
Check(feedbackTextures.Requests.Last().Value == "effects.blocked.png");
blockedCharacter.Update(0.5d);
Check(!blockedCharacter.IsShowingFeedback && blockedCharacter.Renderer.IsVisible);
blockedCharacter.Health = 1;
NexusRealms.Prelude.Combat.DamageResolver.Apply(blockedCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 }), impact: new("effects.impact.png"));
blockedCharacter.Update(0.1d);
Check(!blockedCharacter.IsShowingDamage && blockedCharacter.IsShowingFeedback && blockedCharacter.Renderer.IsVisible);
blockedCharacter.Update(0.4d);
Check(blockedCharacter.IsShowingFeedback && blockedCharacter.Renderer.IsVisible);
blockedCharacter.Update(0.3d);
Check(!blockedCharacter.IsShowingFeedback && !blockedCharacter.Renderer.IsVisible);
Console.WriteLine("Target attack overlays, blocked hits, timing and lethal feedback checks passed.");

// Exercise the real lifecycle traversal rather than updating characters directly.
var lifecycleScene = new Scene { MainCamera = new Nexus.Graphics.Cameras.StaticCamera() };
var lifecycleFormation = new CharacterFormation(new(1000f, 800f));
var lifecycleCharacter = new Character(new CharacterData
    { Id = "lifecycle", Name = "Lifecycle", Artwork = new("test"), Portrait = new("test") }, texture, texture);
lifecycleCharacter.ConfigureEffectOverlays(feedbackTextures, 1);
lifecycleFormation.SetSlot(FormationSlot.FrontCenter, lifecycleCharacter);
lifecycleScene.Children.Add(lifecycleFormation);
var gameSystem = new GameSystem(new Nexus.Core.Events.EventHub(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GameSystem>.Instance);
gameSystem.LoadScene(lifecycleScene);
gameSystem.Initialize();
gameSystem.Update(0d);
var originalTint = lifecycleCharacter.Renderer.Instances.Values.Single().Color;
NexusRealms.Prelude.Combat.DamageResolver.Apply(lifecycleCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1, [DamageType.Fire] = 1 }),
    impact: new("effects.slash.png"));
var physicalTint = lifecycleCharacter.Renderer.Instances.Values.Single().Color;
Check(!physicalTint.Equals(originalTint));
gameSystem.Update(0.079d);
Check(lifecycleCharacter.Renderer.Instances.Values.Single().Color.Equals(physicalTint));
gameSystem.Update(0.001d);
Check(!lifecycleCharacter.Renderer.Instances.Values.Single().Color.Equals(physicalTint));
gameSystem.Update(0.08d);
Check(lifecycleCharacter.Renderer.Instances.Values.Single().Color.Equals(originalTint));
Check(!lifecycleCharacter.IsShowingDamage && lifecycleCharacter.IsShowingFeedback);
gameSystem.Update(0.34d);
Check(!lifecycleCharacter.IsShowingFeedback);
NexusRealms.Prelude.Combat.DamageResolver.Apply(lifecycleCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.Basic] = 3 }), impact: new("effects.impact.png"));
Check(lifecycleCharacter.Health == 0);
gameSystem.Update(0.499d);
Check(lifecycleCharacter.Renderer.IsVisible);
gameSystem.Update(0.001d);
Check(lifecycleCharacter.Renderer.IsVisible && lifecycleCharacter.IsShowingFeedback);
gameSystem.Update(0.125d);
Check(lifecycleCharacter.Renderer.Instances.Values.Single().Color.A < 1f);
Check(lifecycleCharacter.Renderer.IsVisible);
gameSystem.Update(0.125d);
Check(ReferenceEquals(lifecycleFormation[FormationSlot.FrontCenter], lifecycleCharacter));
Check(lifecycleCharacter.Health == 0 && lifecycleCharacter.Definition.Name == "Lifecycle");
Check(!lifecycleCharacter.HitTest(lifecycleCharacter.Position));
Check(!lifecycleCharacter.Renderer.IsVisible && !lifecycleCharacter.IsShowingFeedback);
Console.WriteLine("Game loop advances 80 ms damage points, 500 ms overlays, and hides defeated characters.");

// Atlas frames retain character geometry and select only the currently displayed pixels.
var sheetPath = Path.Combine(Path.GetTempPath(), $"character-animation-{Guid.NewGuid():N}.json");
try
{
    File.WriteAllText(sheetPath, """
        {"frames": {
          "actor (Idle) 0.aseprite": {"frame":{"x":0,"y":0,"w":2,"h":2},"rotated":false,"trimmed":false,"duration":100},
          "actor (Stagger) 0.aseprite": {"frame":{"x":2,"y":0,"w":2,"h":2},"rotated":false,"trimmed":false,"duration":100},
          "actor (Stagger) 1.aseprite": {"frame":{"x":4,"y":0,"w":2,"h":2},"rotated":false,"trimmed":false,"duration":100}
        },"meta":{"size":{"w":6,"h":2},"frameTags":[]}}
        """);
    var atlas = new Texture(new("atlas"), 6, 2, Enumerable.Range(0, 12)
        .Select(index => index % 6 < 2 ? new Color(1f, 1f, 1f, 1f) : new Color(0f, 0f, 0f, 0f)).ToArray());
    var animated = new Character(new CharacterData
        { Id = "animated", Name = "Animated", Artwork = new("atlas"), Portrait = new("atlas") }, atlas, texture);
    animated.ConfigureAnimations(AsepriteSpriteSheet.Load(sheetPath));
    var sprite = animated.Renderer.Instances.Values.Single();
    Check(animated.VisualWidth == 2 && animated.VisualHeight == 2);
    Check(animated.HitTest(new(0f, -1f)));
    NexusRealms.Prelude.Combat.DamageResolver.Apply(animated.DamageReceiver,
        new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 }));
    Check(sprite.TexCoord.X == 2f / 6f && !animated.HitTest(new(0f, -1f)));
    animated.Update(0.1d);
    Check(sprite.TexCoord.X == 4f / 6f && animated.IsShowingFeedback);
    animated.Update(0.1d);
    Check(sprite.TexCoord.X == 0f && !animated.IsShowingFeedback && animated.HitTest(new(0f, -1f)));
    animated.ConfigureEffectOverlays(feedbackTextures, 1);
    NexusRealms.Prelude.Combat.DamageResolver.Apply(animated.DamageReceiver,
        new(new Dictionary<DamageType, int> { [DamageType.Fire] = 1 }), impact: new("effects.slash_01.png"));
    Check(sprite.TexCoord.X == 0f && animated.IsShowingDamage);
    var effectRenderer = animated.Components.OfType<SpriteRenderer>()
        .Single(renderer => renderer.Texture?.ContentId == texture.ContentId && renderer.DrawOrder == 90);
    Check(effectRenderer.Instances.Values.Single().Size.X == animated.VisualWidth * 0.5f);
    animated.Update(0.483d);
    Check(sprite.TexCoord.X == 0f && effectRenderer.IsVisible);
    animated.Update(0.001d);
    Check(sprite.TexCoord.X == 2f / 6f && effectRenderer.IsVisible);
    animated.Update(0.015d);
    Check(effectRenderer.IsVisible); // Still visible at 499 ms.
    animated.Update(0.001d);
    Check(sprite.TexCoord.X == 2f / 6f && !effectRenderer.IsVisible);
    animated.Update(0.184d);
    Check(sprite.TexCoord.X == 0f && !animated.IsShowingFeedback);
    // An update crossing both effect phases advances stagger only after its cue.
    NexusRealms.Prelude.Combat.DamageResolver.Apply(animated.DamageReceiver,
        new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 }), impact: new("effects.slash_01.png"));
    animated.Update(0.5d);
    Check(sprite.TexCoord.X == 2f / 6f && !effectRenderer.IsVisible);
    animated.Update(0.184d);
    Check(sprite.TexCoord.X == 0f && !animated.IsShowingFeedback);
    animated.StaggerAnimationEnabled = false;
    animated.Health = 10;
    NexusRealms.Prelude.Combat.DamageResolver.Apply(animated.DamageReceiver,
        new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 }));
    Check(sprite.TexCoord.X == 0f && animated.IsShowingDamage);
    animated.Update(0.08d);
    Check(sprite.TexCoord.X == 0f && !animated.IsShowingFeedback);
    NexusRealms.Prelude.Combat.DamageResolver.Apply(animated.DamageReceiver,
        new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 }), impact: new("effects.slash_01.png"));
    animated.Update(0.484d);
    Check(sprite.TexCoord.X == 0f && effectRenderer.IsVisible);
    animated.Update(0.017d);
    Check(sprite.TexCoord.X == 0f && !animated.IsShowingFeedback);
    animated.StaggerAnimationEnabled = true;
    animated.Health = 1;
    NexusRealms.Prelude.Combat.DamageResolver.Apply(animated.DamageReceiver,
        new(new Dictionary<DamageType, int> { [DamageType.Basic] = 1 }), impact: new("effects.slash_01.png"));
    animated.Update(0.5d);
    Check(animated.Renderer.IsVisible && sprite.Color.A == 1f);
    animated.Update(0.184d);
    animated.Update(0.25d);
    Check(!animated.Renderer.IsVisible && !animated.IsShowingFeedback);
    Console.WriteLine("Atlas idle, stagger, hit testing and lethal feedback checks passed.");
}
finally { File.Delete(sheetPath); }

var coneCombat = new CombatSystem();
coneCombat.Add(new("cone-player", true));
foreach (var slot in Enum.GetValues<FormationSlot>())
{
    var id = $"cone-{slot}";
    coneCombat.Add(new(id, false, 10, team: CombatTeam.Enemies));
}
coneCombat.Add(new("cone-ally", false, 10, team: CombatTeam.PlayerAndAllies));
var coneResolver = new CombatActionResolver(coneCombat, new Dictionary<StatusEffectId, StatusEffectData>());
coneResolver.Add(new("cone-player", true, CombatTeam.PlayerAndAllies, new(20), CombatLoadout.CreatePlayerLoadout()));
foreach (var slot in Enum.GetValues<FormationSlot>())
{
    var id = $"cone-{slot}";
    coneResolver.Add(new(id, false, CombatTeam.Enemies, new(20), new(), slot));
}
coneResolver.Add(new("cone-ally", false, CombatTeam.PlayerAndAllies, new(20), new(), FormationSlot.MiddleCenterLeft));
var coneTargets = new TargetingProfile(TargetShape.Cone)
    .Select(coneResolver, "cone-player", null);
Check(coneTargets.ToHashSet(StringComparer.Ordinal).SetEquals(
    ["cone-FrontCenter", "cone-MiddleCenterLeft", "cone-MiddleCenterRight", "cone-BackLeft", "cone-BackCenter", "cone-BackRight"]));
Check(new TargetingProfile().Relationship == TargetRelationship.OpposingTeam);
var allEnemies = new TargetingProfile(TargetShape.All)
    .Select(coneResolver, "cone-player", null);
Check(allEnemies.Length == 10 && !allEnemies.Contains("cone-player") && !allEnemies.Contains("cone-ally"));
var allCharacters = new TargetingProfile(TargetShape.All, Options: TargetingOptions.AllTeams)
    .Select(coneResolver, "cone-player", null);
Check(allCharacters.Length == 12 && allCharacters.Contains("cone-player") && allCharacters.Contains("cone-ally"));
var frontRow = new TargetingProfile(TargetShape.FrontRow)
    .Select(coneResolver, "cone-player", null);
Check(frontRow.ToHashSet(StringComparer.Ordinal).SetEquals(
    ["cone-FrontLeft", "cone-FrontCenter", "cone-FrontRight"]));
var middleRow = new TargetingProfile(TargetShape.MiddleRow)
    .Select(coneResolver, "cone-player", null);
Check(middleRow.ToHashSet(StringComparer.Ordinal).SetEquals(
    ["cone-MiddleLeft", "cone-MiddleCenterLeft", "cone-MiddleCenterRight", "cone-MiddleRight"]));
var backRow = new TargetingProfile(TargetShape.BackRow)
    .Select(coneResolver, "cone-player", null);
Check(backRow.ToHashSet(StringComparer.Ordinal).SetEquals(
    ["cone-BackLeft", "cone-BackCenter", "cone-BackRight"]));
var shortMelee = new ActionData
{
    Id = "short-melee",
    MeleeRange = WeaponRange.Short,
    Targeting = new(TargetShape.Melee),
};
Check(shortMelee.SelectTargets(coneResolver, "cone-player", null).IsEmpty);
Check(shortMelee.SelectTargets(coneResolver, "cone-player", "cone-FrontLeft")
    .SequenceEqual(["cone-FrontLeft"]));
Check(shortMelee.SelectTargets(coneResolver, "cone-player", "cone-MiddleLeft").IsEmpty);
var mediumMelee = shortMelee with { Id = "medium-melee", MeleeRange = WeaponRange.Medium };
Check(mediumMelee.SelectTargets(coneResolver, "cone-player", "cone-MiddleLeft")
    .SequenceEqual(["cone-MiddleLeft"]));
Check(mediumMelee.SelectTargets(coneResolver, "cone-player", "cone-BackLeft").IsEmpty);
var longMelee = shortMelee with { Id = "long-melee", MeleeRange = WeaponRange.Long };
Check(longMelee.SelectTargets(coneResolver, "cone-player", "cone-BackLeft")
    .SequenceEqual(["cone-BackLeft"]));
var elevatedCombat = new CombatSystem();
elevatedCombat.Add(new("reach-player", true));
elevatedCombat.Add(new("reach-target", false, team: CombatTeam.Enemies));
var elevatedResolver = new CombatActionResolver(elevatedCombat, NexusRealms.Prelude.Database.StatusEffects.All);
elevatedResolver.Add(new("reach-player", true, CombatTeam.PlayerAndAllies, new(20), new()));
elevatedResolver.Add(
    new("reach-target", false, CombatTeam.Enemies, new(20), new(), FormationSlot.FrontLeft),
    ["elevated"]
);
Check(shortMelee.SelectTargets(elevatedResolver, "reach-player", "reach-target").IsEmpty);
Check(mediumMelee.SelectTargets(elevatedResolver, "reach-player", "reach-target")
    .SequenceEqual(["reach-target"]));
Check(
    NexusRealms.Prelude.Database.Progression.Aries.Rampage.Targeting?.Shape == TargetShape.FrontRow
        && NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind.Targeting?.Shape
            == TargetShape.FrontRow
);
var coneAction = new ActionData { Id = "cone-action", Targeting = new(TargetShape.Cone) };
var coneLoadout = CombatLoadout.CreatePlayerLoadout();
coneLoadout.AddActionCommand(coneAction);
Check(!coneLoadout.Commands.Single(command => command.Id.Value == coneAction.Id).RequiresTarget);
var frontRowAction = new ActionData { Id = "front-row-action", Targeting = new(TargetShape.FrontRow) };
coneLoadout.AddActionCommand(frontRowAction);
Check(!coneLoadout.Commands.Single(command => command.Id.Value == frontRowAction.Id).RequiresTarget);
foreach (var shape in new[] { TargetShape.MiddleRow, TargetShape.BackRow })
{
    var rowAction = new ActionData { Id = $"{shape}-action", Targeting = new(shape) };
    coneLoadout.AddActionCommand(rowAction);
    Check(!coneLoadout.Commands.Single(command => command.Id.Value == rowAction.Id).RequiresTarget);
}
Console.WriteLine("Cone and row targeting select the authored formations without an anchor.");

sealed class FeedbackTextures(ITexture texture) : ITextureRegistry
{
    public List<ContentId> Requests { get; } = [];
    public ITexture GetOrCreate(ContentId id) { Requests.Add(id); return texture; }
    public ITexture Get(TextureId id) => texture;
    public void Reset() { }
    public void Dispose() { }
}
