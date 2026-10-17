# Action effects and progression

`DataModel` owns authored definitions. It has no references to the `Combat` or
`Actions` runtime namespaces. `CarriedItem`, `DamageProfile`, formation enums,
`TargetingProfile`, `ActionData`, effect records, status definitions, and scenario
objectives/events are data. Mutable `GameState` belongs to the Prelude runtime.
Scenario adapters turn typed objectives and events into executable behavior.
`ActionData.Flags` describes action properties such as Visual, Magical, or Melee;
`TargetingProfile` describes recipient selection independently.

An action is a record class derived from `ActionData` or `EffectAction`. For example:

```csharp
public sealed record ExampleStrike : EffectAction
{
    public ExampleStrike() : base("example-strike",
        new DamageEffect(DamageType.Slashing, 2),
        new DelayEffect(0.5f))
    {
        Classifications = ["weapon"];
        Requirements = new() { Equipment = [new("Right hand")] };
        Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)];
        Targeting = new(TargetShape.Single);
    }
}
```

This is an authoring example, not a new balanced ability. `CharacterData.Actions`
declares the character's complete capability list. `AbilityData.Actions`
declares capabilities unlocked by that progression node. `CombatLoadout.Learn`
records the learned ability and registers its action classes with a required
ability condition. All required abilities, owned items, and equipment associations
must match. Unequipping disables the action without unlearning the ability.
Granting a capability does not learn progression; its existing requirements remain.
`CommandDefinition` is the compatibility HUD adapter, with an `ActionId` distinct
from `AbilityId`. Dragging a progression icon cannot learn it or invent an action.

`CombatActionResolver.Commit` checks capability identity, current restrictions,
requirements, targeting, and aggregate costs before scheduler commitment. Costs
require full availability and remain paid when effects are evaded or negated.
`ResolveCommitted` is the adapter for effects of actions already committed by
existing weapon/throw playback or authored timeline events; it never pays costs
or repeats availability checks. It should not be used to accept a new player action.

The sixteen operation classes are Damage, Restore, Delay, Resist, Negate, Evade,
Move, Restrict, Modify, Consume, Grant, Remove, Apply, Dispel, Transfer, and
Prevent, each
with an `Effect` suffix. Their subjects and numerical fields are strongly typed.
Resist/Negate/Prevent/Evade/Restrict/Modify are persistent rules installed through
a status's `Rules` or an explicit equipment/innate rule collection; authoring them
as transient action operations is rejected because they have no defined lifetime.

Defaults are explicit:

- Resolve in authored order. An unavailable operation reports unchanged without
  cancelling independent effects. `RequiresEffect` names an earlier operation
  which must have changed that same recipient before a dependent effect proceeds.
- Freeze recipients at resolution start. Single, self, row, column, adjacent,
  cone, and all are targeting profiles. Row, column and adjacent use a selected
  target as their anchor; cone selects front-center, both middle-center slots,
  and all three back-row slots without an anchor. Targeting defaults to the
  opposing team; `TargetingOptions.AllTeams` explicitly includes both teams,
  including the source. Movement does not recalculate recipients.
- Source-scoped effects express execution commitments independent of target
  evasion. Each target's evasion cancels every target-scoped operation for that
  recipient while other targets continue.
- A modification filter defaults to rules owned by its recipient; source-scoped
  filters read rules on the acting character and may require action flags. A
  source damage modifier creates a missing typed damage entry once per attack
  index, so multi-weapon attacks receive the bonus for each strike.
- Resolve each damage type independently. Resistance reductions add and share a
  per-action, per-type budget across split damage entries. Negation overrides
  numerical changes. Outcomes retain requested amount, resolved damage, and
  actual Health loss separately, together with rule instance/source attribution.
- Numerical modification is signed addition, summed and clamped once. Supported
  fields are Damage amount, Restore amount, Delay amount, and status duration.
  Integer fields reject fractional adjustments. Targeting extent and unsupported
  operations cannot be modified. Other numerical fields remain additive. Persistent `TurnCostMultiplierEffect` rules multiply together to scale committed Turn costs.
- Delay only affects an existing future occurrence. It cannot postpone an
  already active action. Movement rejects player/occupied positions and updates
  the formation and scheduler row without an implicit swap or displacement.
- Restore has no implicit resurrection and no gameplay maximum; starting Health is not a cap.
- Consume effects reduce up to the available Focus/item quantity. Grant follows
  identity and inventory capacity rules and never implicitly equips items.
- Remove distinguishes owned items, equipment associations, and capabilities.
  Unequipping requires inventory space. Removing items clears their associations.
- Resource transfers require the full amount and validate capacity before either
  endpoint changes. Item transfers move whole stacks, preserving identity and
  weapon ammunition state; they reject duplicate identities/full destinations
  and never automatically equip the destination item.

Every status definition declares Reject, Refresh, Replace, or Stack. Refresh
retains application order and resets source/lifetime/uses; Replace receives a new
application order. Stack creates independent instances. Persistence explicitly
names permanent, absolute timeline, or recipient-turn-start clocks. A duration
of 1 with `RecipientTurnStart` expires before selection at the next recipient turn.
Bounded uses declare incoming action or non-evaded incoming action, optionally
filtered. Dispel uses explicit identifiers/tags; limited selection removes oldest
applications first. With DurationReduction, Dispel instead shortens matching timed statuses by that many turns, removing exhausted instances and leaving permanent lifetimes unchanged. Natural expiration does not undo earlier effects.

Existing status presentation placeholders remain mechanically empty; their names
imply no resistance, Focus recovery, or other hidden behavior. Existing attacks
retain their authored damage through a compatibility projection. No new balance
values or progression branches are installed by this framework.

Run `dotnet run --project tests/ActionEffectsChecks` for acceptance and lifecycle
checks. The existing combat, target-selection, story, ability-drag, and simulation
checks verify the adapters against gameplay code rather than linked source copies.
