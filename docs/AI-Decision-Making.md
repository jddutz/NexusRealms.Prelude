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

All policies share damage, elimination, preservation, resource and Turn costs;
preferences do not require a matching effect or an ally before pursuing victory.
Newly legal opposing choices are not penalized merely for existing: additional
Health exposure is measured by the local resolved threat estimate. Useful denied
actions and crossed scheduling opportunities still reward disruption.

Authored victory and defeat conditions also supply `objective-victory-progress`
and `objective-defeat-progress` features to every policy. The shared
`ObjectiveProgress` weight defaults to 10 per thousandth of progress: completely
fulfilling one condition contributes 10,000 utility before ordinary tactical
features. Progress is the average fractional Health reduction of the condition's
recipients, relative to their Health at the current decision boundary, clamped
between zero and completion. Surplus healing cannot manufacture objective progress.
Player
policies reward victory progress and penalize defeat progress; enemies reverse
those signs. `CharacterDefeated`, `AllEnemiesDefeated`, and `PlayerDefeated` each
derive their recipients from objective data. There are no item or character-name
preferences. Actual terminal victory always outranks utility, regardless of weights.
Equipment changes and reloads also score the difference in the best achievable
immediate follow-up utility, including that follow-up's costs. The symmetric
`prepared-action-value` contribution rewards enabling a useful attack and penalizes
losing it. This bounded probe uses legal actions and ordinary resolution, without
special rules for named weapons or a fallback to Strike. It does not advance the
scheduler or predict whether the actor survives until its next decision. It does
not search further preparation sequences; Analytical remains the scheduled planner.
Chaotic cycles its ordered `Modes` after a successful committed action. Evaluating
or previewing never advances a cursor; switching policies preserves the cursor.
Reactive observes committed classifications and resolved outcomes in its
`HistoryWindow`. Stable rules match repeated restoration, repeated `ranged`
actions, then repeated damage, choosing Disruptive or Defensive respectively.
Without a match it uses `Fallback`. No pattern is inferred from an action's name.

Analytical uses a deterministic beam search through real scheduled events and
combat decisions. It branches on future choices by the deciding actor instead of
assuming that actor will follow a greedy continuation. `DecisionDepth` counts that
actor's decisions, including the root (default 3); intervening enemy and allied
actions do not consume depth. Their responses use the shared immediate evaluator
with `OpposingContinuation` and `Continuation`, respectively. These responses are
policy assumptions, not an adversarial search over every possible response.

`BeamWidth` limits retained sequences at each layer (default 8). Every legal
candidate in a layer is scored before pruning, so equipment candidates cannot
consume the budget before later attack candidates are compared. `NodeBudget`
counts candidate resolutions and scheduler occurrences (default 8192). If a
layer is incomplete, only the last fully completed layer competes. If no layer
completed, `Fallback` performs immediate evaluation outside the search budget.
Terminal outcomes outrank utility; nonterminal sequences compare net consequences
and cumulative Turn expenditure by the deciding actor. Traces report completed
depth, explored nodes, beam width, pruning, truncation, and fallback. Beam pruning
can discard a useful setup; this remains bounded planning, not an exhaustive
proof of optimal play.

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
