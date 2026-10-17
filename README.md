# NexusRealms: Prelude

Prelude is a game built to exercise the Nexus game engine. It is a classic 2D RPG, with a dialogue-driven storyline and simple combat mechanics.

Authored combat definitions belong to `DataModel`: character action capabilities,
progression abilities, `TargetingProfile`, typed effects, items, and status definitions.
Runtime resolution in `Actions` uses the existing health, loadout, and scheduler owners.
Abilities unlock action classes subject to their item and equipment requirements;
learning an ability and selecting an action are separate operations. See
[action authoring](docs/Action-Effects.md) for composition and lifetime rules.

Zodiac progression uses parent prerequisites, not directional groups; authored
node order does not constrain learning. Multiple abilities can be learned per
visit, with availability refreshed after each purchase. Continue unlocks after
learning an ability, or immediately when points or unlearned abilities run out.
The story starts at `Chapter1.Part1.Begin`; Continue follows its authored
`NextNode` directly to the intro encounter, without generated progression visits.

The headless `Prelude/Simulator` uses the same action resolver. Mutable `GameState`
lives in the Prelude runtime; scenario objectives and events remain data definitions
and are converted into runtime behavior by adapters.

Each `CharacterData` owns its AI strategy. Combat and simulation share complete
legal candidates, isolated previews, deterministic policies and bounded Analytical
rollouts. See [character AI decisions](docs/AI-Decision-Making.md) for configuration,
observation assumptions, memory, diagnostics and acceptance checks.

Combat simulation is an on-demand workflow available through `$nexusrealms-simulate`,
separate from routine ability implementation and focused checks. See
[simulator usage](Simulator/README.md) for manual commands and output details.

The simulator cycles through every zodiac sign and configurable player strategy
instead of repeating one policy or choosing random actions; `playerZodiacs` and
`playerStrategies` in the state file can limit either cycle. By default, it runs
once per zodiac-strategy combination; explicit run counts repeat the cycle with
additional seeds. A requested full scenario matrix uses one complete
cycle (currently 120 runs), without `--runs`; repeated seeds are reserved for
explicit randomness investigations. Each result records its zodiac and strategy, and the report
compares outcomes by both dimensions. The JSON output keeps shared
scenario and enemy configuration at the top level, with player configurations
keyed by `ZodiacSignId`. Each player learns every ability in that sign's
`ProgressionGraph`, plus any explicit `learnedAbilities`; normal action equipment
and resource requirements still apply. Placeholder abilities without authored
actions remain learned but do not create executable actions. Each run records
ending resources, stable deciding-action identifiers, action uses, and
aggregated action effects by target. The console log retains the detailed
decision trace, considered candidates, rationale, and resulting effects.
The `.scripts/RunSimulator.ps1` launcher writes a compact Markdown outcome and
strategy comparison beside its JSON and diagnostic log. Direct simulator
invocations can use `--report <summary.md>`; `--output <results.json>` remains
independently optional.

Prelude starts with `ZodiacSelectionScene`, where the player selects one of twelve
signs before entering the authored opening progression visit. Runtime zodiac
selection and progression graph keys use `ZodiacSignId`; `ZodiacSign` holds
descriptive character-creation data, including element and modality.

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

Slow and Haste share persistent Turn-cost multipliers (defaults 1.5x and 0.75x). Active multipliers multiply together, and the cost is fixed when an action is committed. Existing queued turns, status durations, and Focus regeneration retain their timeline timing. Wait still schedules just after the next character. Status applications choose their duration.

Fixed-duration defaults allow time to act after casting: Slow/Haste, Headwind, Cooling Breeze and planned Wind Shear last 3 timeline turns; Blind, Bewilder and planned Silence last 2. Focus-based Levitate and Pressure Shield durations remain unchanged.

Basic damage (`DamageType.Basic`) is the uncategorized base damage type. Hurricane deals 2 Basic and 1 Electrical damage to every enemy using the shared action resolver.

Silence applies a negative, refreshable Silenced status to one Visual enemy target for 2 timeline turns (1 Turn / 0 Focus). It blocks the Magical targeting flag, representing magical affinity regardless of speech or Focus cost. Visual and Magical combine independently. Aquarius spells carry Magical; Whirlwind and ordinary weapon attacks do not. Cleanse shortens Silence and Purge removes it.
