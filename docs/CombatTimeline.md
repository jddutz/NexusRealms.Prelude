# Combat timeline

`CombatSystem` owns one encounter. Add combatants in authored order, assign
`DecideAction`, register effects, then call `Process()`. Processing jumps to
scheduled timestamps and stops at player input. Call `SubmitAction` with a
positive finite cost and an effect callback to resolve that action and continue.
The scene exposes this API; hand selection alone does not submit an action.
The current scene AI passes at a cost of 0.6 until authored decisions exist.

The order is exact float Turn, ascending integer priority, then insertion
sequence. Effects default to priority 0; combatants use 100. Moving an occurrence
preserves its sequence. Freshly scheduled occurrences get a new sequence.
`AdjustTurn` clamps at CurrentTurn and only moves that combatant's opportunity.
Use system APIs for combat scheduling so callback and combatant bookkeeping stay
consistent. Combatant identifiers identify encounter instances, not definitions.

`ScheduleEvent` returns a cancellation handle. A recurring callback schedules
one successor at CurrentTurn + interval. A duration schedules one expiration;
remove the effect early with `CancelEvent(handle)`. Sources own their effect
handles and must cancel them on removal. `Remove` cancels a combatant's turn.
`End` clears all pending events; retreat invokes it. `IsCombatOver` lets combat
rules terminate processing before another occurrence or after an action.

`Process` resolves at most 1024 occurrences per call (configurable). Repeated
same-time authored effects therefore cannot hang a call. Hosts can explicitly
continue processing after a budget yield. Action costs must actually advance
the float representation, including at very large timeline positions.

`Timeline.Preview(count)` returns a bounded, ordered snapshot of currently
scheduled occurrences, with labels, IDs, timestamps and optional event icons.
It does not predict future actions. The scene shows the active portrait followed
by up to seven queued occurrences and refreshes on model changes.

Run the dependency-free checks with:

    dotnet run --project tests/CombatTimelineChecks

Build the game with:

    dotnet build Prelude/Prelude.csproj

Occurrences and authored encounter events expose `IsVisible` (default true)
and `PresentationKey`. Visibility only affects rendering: hidden events still
execute in the same deterministic order. `Preview(count, visibleOnly: true)`
filters before applying the preview limit. `Timeline.SetVisibility` updates a
pending event and refreshes the UI.

The scene's `TimelineRenderer.Register(key, factory)` selects a custom UI factory
for events scheduled with that presentation key. Factories receive the occurrence
and active flag and return a fresh Element; the strip positions it at its Turn.
For example, register an explosion factory and schedule with
`presentationKey: "explosion"`. Model callbacks remain independent of GUI types.
The fallback draws combatant portraits, a supplied Icon, or the event Label.

Combatant ties use higher Initiative first, then PlayerAndAllies before Enemies,
then Front, Middle, Back, then a random rank assigned once on encounter entry.
`CombatSystem(seed)` makes this rank reproducible and preserves it on subsequent
turns. Explicit event priorities still determine effects versus combatant turns;
sequence is the final fallback. Initiative belongs to character definitions and
the player GameState; team belongs to encounter placements, and row comes from
the formation slot.

The strip reserves a distinct slot per occurrence, including ties, and distributes
remaining width according to relative Turn. All visible combatants are included;
only non-character events are limited to seven. Integer boundary markers use the
same time-gap mapping and stop before the last visible occurrence.

Target selection is presentation state: selecting does not submit a combat action.
Primary-pointer click/release on the same character focuses it; empty world space
clears focus. Hit testing maps through the actual Fill viewport and checks sprite
alpha, choosing the frontmost drawn character. HUD and modal interactions do not
select world characters.

`CombatScene.FocusCharacter(character)` also supports programmatic selection.
`FocusedCharacter` exposes the current target; `FocusChanged` receives the new
character (or null). Each Character exposes `IsFocused`, `IsValidTarget`, and
`FocusChanged`, which fires when its focused state changes. Front-row targets use
`ui.selection_indicator.png`; other rows use `ui.invalid_selection.png`.
Indicators follow scale and elevation above the character's head. Ending combat
clears focus. These validity rules are provisional until authored abilities exist.

Run focus-transition and opaque-pixel hit-test checks with:

    dotnet run --project tests/TargetSelectionChecks

Character visuals use 75% of the prior formation scale. Each character's
screen-space overhead display shows red Health and blue Focus segments beside
the selection symbol, with fixed-size icons and bars. CharacterData supplies the
initial values (defaults: Health 5, Focus 2). Runtime Character.Health and
Character.Focus accept nonnegative values and raise ResourcesChanged on changes.
The overhead display reads those current values and follows scaling, elevation,
and the actual world viewport. Selecting a target remains independent of its
Focus resource.
