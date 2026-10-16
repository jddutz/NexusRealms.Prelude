# Combat simulation

From the repository root:

```powershell
.\.scripts\RunSimulator.ps1
.\.scripts\RunSimulator.ps1 -Seed 42
```

The script displays the console output and saves it to a timestamped `.log` file
alongside detailed per-run `.json` results in `.temp/simulator-results/` (ignored by Git).
Each invocation uses a fresh seed unless `-Seed` is supplied; the summary prints
the seed for replay. Optional `-State` and `-OutputDirectory` parameters accept
custom paths. The script can also be invoked from another working directory.
Use `-NoBuild` to reuse the existing build for repeated runs.

To run directly:

```powershell
dotnet run --project .\Simulator --launch-profile Simulator

dotnet run --project .\Simulator --no-launch-profile -- --state .\Simulator\examples\intro.json
dotnet run --project .\Simulator --no-launch-profile -- --state .\Simulator\examples\intro.json --seed 42 --output results.json
```

By default, the CLI runs each player strategy once per progression graph:
12 zodiac graphs x 10 player-supported strategies = 120 runs. Each player learns
all abilities in the selected graph, plus any explicitly configured `learnedAbilities`.
Normal equipment and resource requirements still apply; placeholder abilities
without authored actions do not become executable actions.
For routine scenario validation, omit `-Runs` and `--runs` to run this single
complete matrix. Repeated cycles with additional seeds are only needed for an
explicit investigation of randomness.
The `Simulator` launch profile uses the same default. In VS Code, select `Debug:Simulator` to launch it.

Each run reconstructs a fresh GameState and encounter. The saved
`currentStoryNodeId` selects an existing authored combat scenario. No window,
textures, or graphics device are created. The project references Prelude to reuse
its gameplay code; graphics assemblies remain transitive build dependencies.

State JSON accepts `health`, `focus`, `statusEffects`, `learnedAbilities`, `allowEscape` (default
false), `zodiac` (the starting sign for direct state use), `playerZodiacs` (the ordered zodiac cycle),
`playerStrategy` (base policy settings), and `playerStrategies` (the ordered strategy cycle).
The CLI cycles through all player-supported strategies by default, excluding `Protective` because
the player has no allies. A smaller cycle can be configured, for example:

```json
{ "playerStrategies": ["Aggressive", "Analytical"] }
```

Each zodiac runs through every configured strategy before advancing to the next sign.
`playerZodiacs` defaults to all twelve signs and can restrict the graphs under investigation.
The report records both the zodiac and strategy used for every run. `playerStrategy` settings are applied to each strategy in
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
same state, seed, code, and runtime to replay results. The console log traces each
character decision in turn order: chosen action and target, item display name,
candidate utility scores, selected score contributions, observed strategy history,
planning limits, resolved effects, and post-action Health/Focus. The simulator
enables tracing; callers that omit a trace callback do not capture candidate
details. Optional JSON stores starting player configurations keyed by zodiac in
`PlayerConfigurations`, and shared enemy configuration at the top level.
Each run retains its outcome, zodiac, strategy,
terminal condition, deciding action/source, action-use counts, ending turn, health,
and operation count.

Validation:

```powershell
dotnet run --project .\tests\SimulationChecks
```
