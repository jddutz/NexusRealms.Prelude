# NexusRealms: Prelude

Prelude is a game built to exercise the Nexus game engine. It is a classic 2D RPG, with a dialogue-driven storyline and simple combat mechanics.

Authored combat definitions belong to `DataModel`: character action capabilities,
progression abilities, `TargetingProfile`, typed effects, items, and status definitions.
Runtime resolution in `Actions` uses the existing health, loadout, and scheduler owners.
Abilities unlock action classes subject to their item and equipment requirements;
learning an ability and selecting an action are separate operations. See
[action authoring](docs/Action-Effects.md) for composition and lifetime rules.

The headless `Prelude/Simulator` uses the same action resolver. Mutable `GameState`
lives in the Prelude runtime; scenario objectives and events remain data definitions
and are converted into runtime behavior by adapters.

Story graphs implementing `IStoryGraph` in `Prelude.Database.Chapter*` namespaces are discovered automatically; their characters and nodes are validated and registered by ID at runtime. The loaded story database uses immutable graph data and frozen lookup dictionaries, while chapter definitions remain authored in C#.

Combat characters are world-space `GameObject2D` entities. Each character owns its
`SpriteRenderer`, while `CharacterFormation` positions those entities from the
combat world's dimensions; GUI elements are not used for character placement.
Combat scenes use a grid-clipped `OrthoCamera` world viewport above an 80px HUD
band, with a separate full-screen `StaticCamera` GUI view. Prelude targets
landscape presentation only; portrait layouts are not supported.

## Development

Requires the .NET 10 SDK and a Nexus engine checkout alongside this repository (for example, `../Nexus`). Prelude references the engine's `Nexus.Runtime` project directly.

From the repository root:

```powershell
dotnet build NexusRealms.slnx
dotnet run --project Prelude/Prelude.csproj
```
