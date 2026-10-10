global using Nexus.Core;
global using Nexus.Game;
global using Nexus.Graphics;
global using Nexus.Graphics.Components;
global using Nexus.Graphics.Textures;
global using Silk.NET.Maths;
using NexusRealms.Prelude;
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
feedbackCharacter.ConfigureAttackFeedback(feedbackTextures, 1);
NexusRealms.Prelude.Combat.DamageResolver.Apply(feedbackCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.General] = 5 }),
    impact: new("effects.ranged.png", "test-ammo"));
Check(feedbackTextures.Requests.Select(id => id.Value).SequenceEqual(new[] { "effects.ranged.png", "test-ammo" }));
Check(feedbackCharacter.IsShowingFeedback && feedbackCharacter.Renderer.IsVisible);
feedbackCharacter.Update(0.25d);
Check(feedbackCharacter.IsShowingDamage && feedbackCharacter.Renderer.IsVisible);
feedbackCharacter.Update(0.2d);
Check(feedbackCharacter.IsShowingFeedback && feedbackCharacter.Renderer.IsVisible);
feedbackCharacter.Update(0.25d);
Check(!feedbackCharacter.IsShowingFeedback && !feedbackCharacter.Renderer.IsVisible && !feedbackCharacter.ShadowRenderer.IsVisible);
var blockedCharacter = new Character(new CharacterData
    { Id = "blocked", Name = "Blocked", Artwork = new("test"), Portrait = new("test") }, texture, texture);
blockedCharacter.ConfigureAttackFeedback(feedbackTextures, 1);
NexusRealms.Prelude.Combat.DamageResolver.Apply(blockedCharacter.DamageReceiver, new(), impact: new("effects.impact.png"));
Check(blockedCharacter.IsShowingFeedback && !blockedCharacter.IsShowingDamage);
Check(feedbackTextures.Requests.Last().Value == "effects.blocked.png");
blockedCharacter.Update(0.25d);
Check(!blockedCharacter.IsShowingFeedback && blockedCharacter.Renderer.IsVisible);
blockedCharacter.Health = 1;
NexusRealms.Prelude.Combat.DamageResolver.Apply(blockedCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.General] = 1 }), impact: new("effects.impact.png"));
blockedCharacter.Update(0.1d);
Check(!blockedCharacter.IsShowingDamage && blockedCharacter.IsShowingFeedback && blockedCharacter.Renderer.IsVisible);
blockedCharacter.Update(0.15d);
Check(blockedCharacter.IsShowingFeedback && blockedCharacter.Renderer.IsVisible);
blockedCharacter.Update(0.25d);
Check(!blockedCharacter.IsShowingFeedback && !blockedCharacter.Renderer.IsVisible);
Console.WriteLine("Target attack overlays, blocked hits, timing and lethal feedback checks passed.");

// Exercise the real lifecycle traversal rather than updating characters directly.
var lifecycleScene = new Scene { MainCamera = new Nexus.Graphics.Cameras.StaticCamera() };
var lifecycleFormation = new CharacterFormation(new(1000f, 800f));
var lifecycleCharacter = new Character(new CharacterData
    { Id = "lifecycle", Name = "Lifecycle", Artwork = new("test"), Portrait = new("test") }, texture, texture);
lifecycleCharacter.ConfigureAttackFeedback(feedbackTextures, 1);
lifecycleFormation.SetSlot(FormationSlot.FrontCenter, lifecycleCharacter);
lifecycleScene.Children.Add(lifecycleFormation);
var gameSystem = new GameSystem(new Nexus.Core.Events.EventHub(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GameSystem>.Instance);
gameSystem.LoadScene(lifecycleScene);
gameSystem.Initialize();
gameSystem.Update(0d);
var originalTint = lifecycleCharacter.Renderer.Instances.Values.Single().Color;
NexusRealms.Prelude.Combat.DamageResolver.Apply(lifecycleCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.General] = 1, [DamageType.Fire] = 1 }),
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
gameSystem.Update(0.09d);
Check(!lifecycleCharacter.IsShowingFeedback);
NexusRealms.Prelude.Combat.DamageResolver.Apply(lifecycleCharacter.DamageReceiver,
    new(new Dictionary<DamageType, int> { [DamageType.General] = 3 }), impact: new("effects.impact.png"));
Check(lifecycleCharacter.Health == 0);
gameSystem.Update(0.249d);
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
Console.WriteLine("Game loop advances 80 ms damage points, 250 ms overlays, and hides defeated characters.");

sealed class FeedbackTextures(ITexture texture) : ITextureRegistry
{
    public List<ContentId> Requests { get; } = [];
    public ITexture GetOrCreate(ContentId id) { Requests.Add(id); return texture; }
    public ITexture Get(TextureId id) => texture;
    public void Reset() { }
    public void Dispose() { }
}
