# Combat simulation

From the NexusRealms directory:

```powershell
dotnet run --project Prelude/Simulator -- --state Prelude/Simulator/examples/intro.json
dotnet run --project Prelude/Simulator -- --state Prelude/Simulator/examples/intro.json --runs 1000 --seed 42 --output results.json
```

Runs default to 1. Each run reconstructs a fresh GameState and encounter. The saved
`currentStoryNodeId` selects an existing authored combat scenario. No window,
textures, or graphics device are created. The project references Prelude to reuse
its gameplay code; graphics assemblies remain transitive build dependencies.

State JSON accepts `health`, `focus`, `statusEffects`, and `allowEscape` (default
false). Omit `items` and `itemLayout` to use Prelude's starting sword. Supply both
as empty collections for an unarmed player. Custom `items` use Prelude's
CarriedItem JSON representation (IDs are objects such as `{ "value": "sword" }`);
`itemLayout` maps equipment names or `Inventory:0` through `Inventory:19` to item
ID strings. Invalid layouts and unknown JSON properties are rejected.

Player decisions sample uniformly from legal command/target combinations, throw
item/target combinations, equipment moves, reloads, and optionally escape. This
is a random baseline, not optimal play; option counts influence its behavior.
AI uses Prelude's EnemyActionState selection and ammunition rules. Allies match
the current game behavior: they consume turns without attacking. Status effects
and ability presentation placeholders gain no new mechanics in the simulator.

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
