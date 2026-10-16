# Ability authoring and execution

An ability is a progression unlock (`AbilityData`); an action is an executable combat capability (`ActionData`). Their IDs are independent. One ability may unlock multiple actions. An ability with no actions is a presentation placeholder and never implicitly attacks.

## Current pipeline

1. Aquarius authors twenty `AbilityData` records containing identity, name, description, icon, and actions. `ProgressionGraph.Abilities` contains only references to those identities, direction, prerequisites, and layout. Graphs are explicit data; the progression UI groups nodes by direction.
2. `Abilities.All` collects authored definitions, including `Aquarius.Definitions`, into a lookup. It does not manufacture definitions from graph nodes. Add agreed combat actions directly to the corresponding Aquarius record. Other Zodiac graphs retain explicitly authored shared placeholder definitions until their abilities are designed.
3. `Storyline` validates definitions, action identities, referenced statuses and abilities, and progression prerequisite chains.
4. `Rules.ProgressionRules.Learn` spends one ability point and enforces prerequisites and one selection per visit. New players start with 20 ability points; direction is display grouping only. `CombatLoadout.Learn` registers actions and adds the ability requirement. Repeated learning is harmless; conflicting commands fail before learning mutates state.
5. The shared resolver checks requirements, restrictions, targets, and costs before commitment. It resolves ordered typed effects, returns structured outcomes, and owns status lifetimes. HUD commands and AI use this same runtime.
6. Simulator `learnedAbilities` restores unlocks from the shared registry. Decision traces and JSON results expose selected actions, resolved effects, resources, and learned IDs. This is a combat starting-state fixture; it does not simulate progression visits or enforce chapter learning rules.

## Supported composition

Actions already support Focus/item costs, timeline costs, self/single/row/column/adjacent/all targeting, damage types, healing, delay, fixed-slot movement, resource changes, status application and dispelling. Status rules support resistance, negation, evasion, action restrictions, numerical modifiers, explicit duplicate behavior, and timeline/recipient-turn/use-based expiration. AI preview uses the same effects without mutating live state.

When adding an ability, define targeting and relationship explicitly: defaults permit any single recipient. Set `Effects` explicitly so a spell cannot inherit the legacy one-point attack. Put persistent rules in status definitions, and specify their lifetime and duplicate policy. Use stable action IDs distinct from ability IDs.

## Deferred until the individual rules are agreed

The existing primitives do not yet express relative row pushes, elevation, a sequence of per-turn Focus gains, reducing durations of already active statuses, or restrictions selected by Focus cost. Front/back-row-only selection also needs an explicit rule beyond the current general row targeting shape. These should receive shared primitives and tests when the relevant ability is defined; do not encode them as simulator-only callbacks.

Aquarius layout coordinates and prerequisites are still placeholders. No ability balance, branch order, duration interpretation, or combat behavior is inferred from its prose description.

## Verification

Run `dotnet run --project Tests/ProgressionChecks`, `dotnet run --project Tests/ActionEffectsChecks`, and `dotnet run --project Tests/SimulationChecks`. These cover learning rules, a synthetic learned action executing through combat, repeat-learning/conflict safeguards, Aquarius save restoration and reporting, and the absence of invented commands for undefined abilities. Add ability-specific acceptance cases as each definition is agreed.

## Rules layer

`NexusRealms.Prelude.Rules` contains static reusable rules. `ProgressionRules` checks and applies learning; `CombatRules` validates authored actions, effects, costs, and status definitions; `HealingRules` applies capped restoration without resurrection; `VictoryRules` and `DefeatRules` build and evaluate encounter objectives. `ScenarioRules` binds authored scenarios to runtime objectives. The combat resolver and scheduler retain runtime orchestration and call these rules.

Rules receive state and definitions explicitly. `GameState` is already a DI singleton per running game and owns its loadout and completed visits. Simulator runs construct independent states. `ProgressionScene` handles navigation after `ProgressionRules.CanContinue` succeeds; there is no progression session object.
