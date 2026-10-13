# Combat simulation

From the repository root:

```powershell
.\.scripts\RunSimulator.ps1
.\.scripts\RunSimulator.ps1 -Runs 1000
.\.scripts\RunSimulator.ps1 -Runs 1000 -Seed 42
```

The script displays the console output and saves it to a timestamped `.log` file
alongside detailed per-run `.json` results in `.temp/simulator-results/` (ignored by Git).
Each invocation uses a fresh seed unless `-Seed` is supplied; the summary prints
the seed for replay. Optional `-State` and `-OutputDirectory` parameters accept
custom paths. The script can also be invoked from another working directory.
Use `-NoBuild` to reuse the existing build for repeated runs.

To run directly:

```powershell
dotnet run --project Simulator --launch-profile Simulator

dotnet run --project Simulator --no-launch-profile -- --state Simulator/examples/intro.json
dotnet run --project Simulator --no-launch-profile -- --state Simulator/examples/intro.json --runs 1000 --seed 42 --output results.json
```

By default, the CLI runs once per configured player strategy: 10 runs with the
default strategy cycle, or one run for each entry in a custom `playerStrategies` list.
Use `-Runs` or `--runs` to request repeated cycles with additional seeds.
The `Simulator` launch profile uses the same default. In VS Code, select `Debug:Simulator` to launch it.

Each run reconstructs a fresh GameState and encounter. The saved
`currentStoryNodeId` selects an existing authored combat scenario. No window,
textures, or graphics device are created. The project references Prelude to reuse
its gameplay code; graphics assemblies remain transitive build dependencies.

State JSON accepts `health`, `focus`, `statusEffects`, `learnedAbilities`, `allowEscape` (default
false), `playerStrategy` (base policy settings), and `playerStrategies` (the ordered strategy cycle).
The CLI cycles through all player-supported strategies by default, excluding `Protective` because
the player has no allies. A smaller cycle can be configured, for example:

```json
{ "playerStrategies": ["Aggressive", "Analytical"] }
```

Each run uses the next strategy in the configured order, wrapping around as needed; the report
records the strategy used for every run. `playerStrategy` settings are applied to each strategy in
the cycle. Omit `items` and `itemLayout` to use Prelude's starting sword and stack of five rocks.
Supply both as empty collections for an unarmed player. Custom `items` use Prelude's
CarriedItem JSON representation (IDs are objects such as `{ "value": "sword" }`);
`itemLayout` maps equipment names or `Inventory:0` through `Inventory:19` to item
ID strings. Invalid layouts and unknown JSON properties are rejected.

Player and enemy decisions use Prelude's configured strategy policies and complete
legal action candidates. The strategy cycle tests deterministic policies rather than choosing
random actions; combat randomness remains controlled by the run seed. For a single policy,
configure `playerStrategies` with one strategy. Allies
match the current game behavior for legacy attacks: they consume turns without
attacking. Explicit typed action definitions use the shared resolver for every team.
Status identifiers expand into independent runtime instances, with behavior
declared by their definitions. Presentation placeholders have no implicit mechanics.

Turn means the fractional combat timeline, not the number of actions. At Turn
100 the encounter is a draw, before any occurrence at or beyond 100 resolves.
An action beginning before 100 resolves using Prelude's existing immediate
action semantics. Victory or defeat before the boundary takes precedence.
Malformed encounters that exhaust an operation budget fail explicitly.

The base seed is printed; run N uses base seed + N (starting at zero). Use the
same state, seed, code, and runtime to replay results. The console report includes
player strategy by outcome and victory rate, deciding action by outcome, player
action totals/averages and use-count distributions, plus a strategy x enemy
configuration x deciding-action cross-tab. A deciding action is the resolution
that reaches a terminal condition; turn-limit draws and initial terminal states
have no deciding action. These are associations, not causal explanations.
Optional JSON records each run's outcome, strategy, enemy configuration, terminal
condition, deciding action/source, action-use counts, ending turn, health, and
operation count.

Validation:

```powershell
dotnet run --project tests/SimulationChecks
```
