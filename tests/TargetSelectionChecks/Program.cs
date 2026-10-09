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
