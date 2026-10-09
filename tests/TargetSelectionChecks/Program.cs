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
var changes = 0;
character.FocusChanged += _ => changes++;
character.SetFocus(true, true);
Check(character.IsFocused && character.FocusRenderer.IsVisible && character.FocusRenderer.Texture == valid);
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
Console.WriteLine("Target selection checks passed.");
