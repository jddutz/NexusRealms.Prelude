# Combat simulation

From the NexusRealms directory:

```powershell
./RunSimulator.ps1
./RunSimulator.ps1 -Runs 1000
./RunSimulator.ps1 -Runs 1000 -Seed 42
```

The script displays the console output and saves it to a timestamped `.log` file
alongside detailed per-run `.json` results in `simulation-results/` (ignored by Git).
Each invocation uses a fresh seed unless `-Seed` is supplied; the summary prints
the seed for replay. Optional `-State` and `-OutputDirectory` parameters accept
custom paths. The script can also be invoked from another working directory.
Use `-NoBuild` to reuse the existing build for repeated runs.

To run directly:

```powershell
dotnet run --project Prelude/Simulator --launch-profile Simulator

dotnet run --project Prelude/Simulator --no-launch-profile -- --state Prelude/Simulator/examples/intro.json
dotnet run --project Prelude/Simulator --no-launch-profile -- --state Prelude/Simulator/examples/intro.json --runs 1000 --seed 42 --output results.json
```

The `Simulator` launch profile runs the intro scenario 100 times with a fresh seed.
In VS Code, select `Debug:Simulator` to launch it with the debugger.

Runs default to 1. Each run reconstructs a fresh GameState and encounter. The saved
`currentStoryNodeId` selects an existing authored combat scenario. No window,
textures, or graphics device are created. The project references Prelude to reuse
its gameplay code; graphics assemblies remain transitive build dependencies.

State JSON accepts `health`, `focus`, `statusEffects`, `learnedAbilities`, and `allowEscape` (default
false). Omit `items` and `itemLayout` to use Prelude's starting sword and stack of five rocks. Supply both
as empty collections for an unarmed player. Custom `items` use Prelude's
CarriedItem JSON representation (IDs are objects such as `{ "value": "sword" }`);
`itemLayout` maps equipment names or `Inventory:0` through `Inventory:19` to item
ID strings. Invalid layouts and unknown JSON properties are rejected.

Player decisions sample uniformly from legal command/target combinations, throw
item/target combinations, equipment moves, reloads, and optionally escape. This
is a random baseline, not optimal play; option counts influence its behavior.
AI uses Prelude's EnemyActionState selection and ammunition rules. Allies match
the current game behavior for legacy attacks: they consume turns without attacking.
Explicit typed action definitions use the shared resolver for every team.
Status identifiers expand into independent runtime instances, with behavior
declared by their definitions. Presentation placeholders have no implicit mechanics.

Turn means the fractional combat timeline, not the number of actions. At Turn
100 the encounter is a draw, before any occurrence at or beyond 100 resolves.
An action beginning before 100 resolves using Prelude's existing immediate
action semantics. Victory or defeat before the boundary takes precedence.
Malformed encounters that exhaust an operation budget fail explicitly.

The base seed is printed; run N uses base seed + N (starting at zero). Use the
same state, seed, code, and runtime to replay results. Output includes outcome
counts, percentages, mean ending turn and health; optional JSON records each
run's seed, outcome, ending turn, health, and operation count. These statistics
support balance assessment but do not automatically determine good design.

Validation:

```powershell
dotnet run --project tests/SimulationChecks
```
