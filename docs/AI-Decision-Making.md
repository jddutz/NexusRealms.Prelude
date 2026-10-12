# Character AI decisions

`CharacterData.Strategy` owns a character's reusable policy. Strategies are not
configured on `CombatScenario` or `CharacterPlacement`. The encounter resolver
binds stable character references to the actual participating characters and
holds independent mutable memory for each combatant. Normal player input is
unchanged; the headless simulator uses a named policy for the simulated player.

```csharp
Strategy = new(StrategyId.Protective) { ProtectAllies = true },
StrategyTransitions =
[
    new("last-stand", new(StrategyConditionKind.HealthAtMost, Health: 2),
        new(StrategyId.Aggressive)),
],
```

Protective accepts `ProtectedIds` containing stable character IDs, or the explicit
`ProtectAllies` selector (all other allies). Unknown, opposing, or ambiguous
references are errors. A defeated protection scope uses `Fallback`, without
changing the character's underlying policy. `$self` is supported in character
transitions. Health thresholds are absolute; no Health maximum is inferred.
The third alley thug demonstrates ally healing, offensive and disruptive actions,
a Protective policy, and a character-owned low-Health transition.

## Policies

All eleven names use version 1 integer weights. Aggressive favors effective
Health loss and ongoing contribution; Reckless increases immediate offense and
discounts its own preservation and resource depletion. Defensive values personal
preservation; Protective values its declared wards; Supportive values the allied
group and useful enabled actions. Disruptive values denied legal actions and
crossed scheduled opportunities. Opportunistic increases elimination preference
and rewards damage types demonstrated to be effective in recent observations.
Cautious increases Focus and item expenditure penalties.

Chaotic cycles its ordered `Modes` after a successful committed action. Evaluating
or previewing never advances a cursor; switching policies preserves the cursor.
Reactive observes committed classifications and resolved outcomes in its
`HistoryWindow`. Stable rules match repeated restoration, repeated `ranged`
actions, then repeated damage, choosing Disruptive or Defensive respectively.
Without a match it uses `Fallback`. No pattern is inferred from an action's name.

Analytical compares bounded deterministic rollouts through real scheduler decisions
and known events. `DecisionDepth` counts committed decisions, including the root.
`NodeBudget` counts simulated candidate resolutions and scheduler occurrences.
`Continuation` and `OpposingContinuation` name nonrecursive immediate policies;
Protective continuation requires a protection scope and is currently rejected.
A predicted player response is a continuation assumption. Only fully evaluated
rollouts compete when the budget runs out. If none completed, `Fallback` performs
immediate evaluation outside the planning budget. Traces identify truncation,
node counts, and the fallback. This is bounded policy rollout, not an exhaustive
proof of the encounter's optimal play or mechanical viability.

## Candidates, resolution and knowledge

`CandidateGeneration` produces complete choices for capabilities, eligible owned
legacy weapons, individual thrown items, hand commands, Wait, reloads, and equipment
changes. Commands are validated and executed by `CombatLoadout`; capabilities and
legacy adapters use the same resolver and weapon preparation as commitment.
NPCs receive only their authored repertoire unless `CharacterData.BasicCommands`
explicitly adds universal hand/Throw/Wait commands. Inventory-only shuffling is
excluded because it creates no tactical option. Equivalent legal equipment choices
remain separate and have stable keys.

`CombatActionResolver.CopyForSimulation` isolates health, resources, inventory cells,
equipment, ammunition, statuses and uses, scheduler entries, objectives, transitions,
overrides, history and mode cursors. Immutable definitions are shared. Preview
executes ordinary commitment, including costs, scheduling and terminal checks,
but stops before pumping the next occurrence. Analytical explicitly advances the
same scheduler in its isolated copy. Live UI animation pays costs and resolves
once at impact, then resumes scheduling after presentation completes.

Use `scenario.ConfigureCombat(combat)` for data-only objectives and
`resolver.ScheduleEvent(eventData)` for authored events. Resolver-owned expiration
callbacks obtain their mutable owners from the supplied simulation. Opaque custom
event/command callbacks or objective predicates are rejected for AI simulation;
they are never executed against captured live objects.

The initial observation model is **fully visible encounters**, including current
inventories, capabilities, statuses, equipment and explicitly known global events.
It does not predict future player input. An authored event with `KnownToAi = false`
is rejected: hidden-information play requires an observation-based simulation
view before it can be supported. `IsVisible` remains a presentation flag, separate
from AI knowledge. Diagnostic traces are not a player-facing information channel.

Immediate policies use a documented local threat estimate: the worst single
observed opposing action against each recipient, acting now without intervening
events. Mitigation and restriction value comes from prospective resolution and
legal action changes, not points assigned to effect verbs. Delay scores crossed
scheduling opportunities, not raw duration. Restoration remains uncapped unless
the runtime character explicitly defines a maximum. Above a reserve of two local
threats, restored Health has one tenth marginal utility; actual restoration in the
preview is never reduced. Longer buff combinations require scheduled Analytical
continuation to establish their value.

Authored terminal victory/defeat outranks ordinary utility, with enemy preference
reversed. Equal objective rank and integer utility use ordinal action ID, target
or anchor ID, item ID, kind and authored choice key. Time costs are quantized to
centiturns with midpoint rounding away from zero. Scheduler ties retain their own
existing rules. `CommitDecision` detects changed live state and reevaluates before
paying costs. `NoLegalAction` reaches the runtime's explicit one-Turn unavailable
action rule; it is not a free Wait and does not advance action memory.

## Runtime changes and diagnostics

Character transitions declare an ID, boundary flags, condition, recipients, priority,
destination and optional rising-edge repetition/cooldown. Conditions at a boundary
share one post-resolution snapshot. Highest priority wins; equal priority uses the
stable transition ID. All eligible once-only transitions are marked handled,
including losers, preventing a losing continuously true transition from undoing
its winner at the next boundary. Applying the batch does not recursively evaluate
its own conditions. History and cursors are retained unless explicitly cleared.

Temporary overrides have a source, priority, application sequence, stable ID and
an expiration time or commit count. Highest priority wins, then latest application,
then ordinal ID. `StatusEffectData.StrategyOverride` follows ordinary status
application, dispel and expiration. Statuses and explicit overrides share one
application sequence. A runtime base change remains underneath an active override.
An action uses the effective policy captured before its resolution; a newly
applied override does not retroactively advance its cursor or consume a use.

`resolver.LastDecision` and the simulator's optional trace callback expose structured
candidate outcomes, score contributions, objective ranks, ties, rejected choices,
observations, policy memory, transitions and planning assumptions. Collections are
canonically ordered for reproducible diagnostics. `Behavior.Save/Load` round-trips
JSON-safe decision memory; `CopyForSimulation` supplies an isolated in-memory combat
checkpoint. Persisting decision memory alone is not a complete combat save: restore
it alongside corresponding scheduler, ownership and status state.

Run the focused acceptance checks with:

```powershell
dotnet run --project tests/AiDecisionChecks
```

The existing action-effects, timeline, story, simulation, target-selection and
ability-drag checks also cover the integration boundaries. Simulation's default
player policy is Aggressive; its seed controls scheduler ranks, not random action
selection. `SimulationState.PlayerStrategy` selects another profile for comparisons.
`CaptureCandidateTraces = false` omits full candidate and rejection payloads without changing evaluation or selection. The simulator enables these payloads when a trace callback is supplied.
