# NexusRealms: Prelude

Prelude is a game built to exercise the Nexus game engine. It is a classic 2D RPG, with a dialogue-driven storyline and simple combat mechanics.

## Development

Requires the .NET 10 SDK and a Nexus engine checkout alongside this repository (for example, `../Nexus`). Prelude references the engine's `Nexus.Runtime` project directly.

From the repository root:

```powershell
dotnet build NexusRealms.slnx
dotnet run --project Prelude/Prelude.csproj
```
