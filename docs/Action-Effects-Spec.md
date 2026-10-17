# Nexus Realms – Prelude: Action Effects

Status: proposed implementation specification  
Date: October 11, 2026

## Purpose and scope

Define the sixteen action verbs through which actions affect combat state or influence the resolution of other actions. These definitions are the mechanical vocabulary for authoring effects. They do not name abilities, prescribe a Zodiac tree, balance encounters, or define Focus recovery.

The player is a single character. Move changes non-player formation positions. Enemy actions may affect multiple allies; the player has no allied party to heal or buff.

The original nine verbs are Damage, Restore, Delay, Resist, Negate, Evade, Move, Restrict, and Modify. The additional seven are Consume, Grant, Remove, Apply, Dispel, Transfer, and Prevent. The boundaries below make overlapping terms explicit. Details not previously settled are proposed defaults in this specification.

## Core terminology

| Term | Meaning |
|---|---|
| Action | What a character chooses and commits to perform. |
| Effect | An operation defined by a verb and its operation-specific parameters. |
| Resolution | Evaluates the committed action against combat state and applies its resulting changes. Evasion, modification, resistance, negation, and outcomes all belong within Resolution. |
| Outcome | The actual result of resolution, including changes, cancellation, and operations that had no effect. |
| Status Effect | A persistent state identifier associated with a character. Its definition identifies the behavior that state enables. |
| Source | The character responsible for the action or persistent behavior. |
| Recipient | The character or state to which an effect applies. A recipient can be the actor. |
| Filter | Explicit criteria determining which actions, effects, damage, or statuses qualify. |

The interaction remains Select Action → Select Target → Confirm → Resolve. Internal evaluation rules are not additional gameplay phases.

Use verbs as operation names. Use nouns for parameters and state. Code may use `DamageEffect`, `RestoreEffect`, and equivalent types; a discriminated definition is also acceptable. Operation-specific parameters must be strongly typed rather than stored in a universal property bag. Status names may be thematic, but their identifiers do not imply undocumented mechanics.

## Verb definitions

| Verb | Exact meaning | Required information |
|---|---|---|
| Damage | Consume the recipient's current Health through damage resolution. | Recipient, nonnegative amount, damage type. |
| Restore | Increase the recipient's current Health. | Recipient, nonnegative amount. |
| Delay | Increase the recipient's scheduled next Turn. | Recipient, nonnegative delay in the scheduler's existing units. |
| Resist | Reduce qualifying incoming Damage by a specified amount. | Protected recipient, damage filter, nonnegative reduction. |
| Negate | Reduce qualifying incoming Damage to zero. | Protected recipient, damage filter. |
| Evade | Cancel all effects of the qualifying incoming action for the protected recipient. | Protected recipient, action filter. |
| Move | Change a non-player recipient's formation position. | Recipient, destination or explicit movement rule. |
| Restrict | Prevent the recipient from using qualifying actions. | Recipient, action filter. |
| Modify | Numerically change a qualifying action's effect parameters during resolution. | Target or source scope, effect filter, optional required action flags, numerical field, numerical operation and value. |
| Consume | Decrease a non-Health consumable resource or item quantity. | Recipient, resource or item reference, nonnegative amount. |
| Grant | Add a non-Health resource, item, or capability to the recipient. | Recipient, typed subject and amount where applicable. |
| Remove | Remove a specified item, equipment association, or capability from the recipient. | Recipient, typed subject reference. |
| Apply | Add a Status Effect to the recipient. | Recipient, StatusEffectId, persistence parameters required by its definition. |
| Dispel | Remove qualifying Status Effects from the recipient. | Recipient, status selector or filter, selection limit if applicable. |
| Transfer | Move an existing resource amount or item from one character to another. | Origin, destination, typed subject and amount where applicable. |
| Prevent | Block application of a specified Status Effect to the recipient. | Protected recipient, StatusEffectId. |

### Damage and Restore

Damage retains its specific Health meaning. Focus loss uses Consume, and Focus recovery uses Grant. Neither is automatically subject to damage resistance. Basic damage has no specialized damage type; implementation may retain the existing representation for this case.

Resolve each typed damage amount independently. Damage cannot reduce Health below zero. Restore cannot reduce Health and does not remove Status Effects. Health has no gameplay maximum; starting Health is not a cap. Restore does not resurrect or remove an incapacitated state.

Record attempted damage, resolved damage, and actual Health loss separately where they differ. An attack against one remaining Health may resolve three damage but consume only one Health.

### Delay

Delay shifts the next scheduled Turn; it does not skip that Turn, remove an action, or change the character's normal scheduling interval. Independent delays add. Preserve the scheduler's existing tie-breaking rule. Do not interpret an amount of one as one round unless that is already the scheduler's unit.

If the recipient has no future Turn, report no change. Delay does not undo an action already committed or resolving. Stun can be authored using Delay without creating a new stun-specific scheduling algorithm.

### Resist, Negate, and Evade

Resist subtracts a fixed quantity from matching incoming damage, with a floor of zero. Proposed default: reductions apply per incoming action and damage type, after same-type damage amounts have been combined. This prevents splitting an identical damage profile into multiple entries from multiplying resistance. Independent reductions add.

Negate sets matching damage to zero. It leaves nonmatching damage and non-damage effects intact. It has no numerical amount to increase. Negate takes precedence over numerical damage changes: a matching negated amount stays zero.

Evade cancels every effect of the incoming action for the protected recipient, including Damage, Delay, Apply, and Move. For an area action, other recipients still resolve normally. An action evaded by its target does not refund actor costs or undo execution commitments such as releasing a thrown weapon.

Filters may select a source, damage type, action classification, or an explicit combination. Multiple criteria within one filter must state whether they mean all or any. A source-and-type filter defaults to both matching. Rule lifetime and uses belong to persistent state, not to the Resist or Negate amount.

### Move

Move changes formation state, rather than merely the rendered sprite position. Validate destinations using formation occupancy and placement rules. Proposed default: an invalid or occupied destination causes no movement. Swapping, pushing occupants, or choosing another destination must be explicitly authored; the resolver must not invent a fallback.

The explicit ShiftRowEffect used by Gale and Tailwind selects the nearest free slot in the adjacent row, with deterministic formation-coordinate and slot-identity tie breaking. Resolve the leading row first to permit followers to use vacated slots. Boundary and full-row recipients remain in place; each recipient shifts at most once. This behavior is specific to the relative-row operation; absolute MoveEffect still rejects an occupied destination.

Move cannot target the player. It does not inherently change Turn, cancel actions, or inflict damage. Subsequent targeting uses the updated formation.

### Restrict

Restrict prevents selection and commitment of matching actions while the restriction applies. Check availability again at commitment against current state. A newly applied restriction does not retroactively cancel an action already resolving.

Restrictions combine by exclusion: an action forbidden by any active restriction is unavailable. Restrict does not remove equipment or capabilities. A restriction representing disarm is mechanically different from Remove applied to an equipped weapon; author the intended behavior explicitly. No named state, including blind or mute, carries additional implicit rules.

### Modify

Modify changes a declared numerical field, such as Damage amount, Restore amount, Delay amount, status duration, or a numerical targeting extent supported by that targeting definition. It does not directly change current Health, Focus, equipment, or Turn.

Each field must explicitly support modification. Damage type, source identity, StatusEffectId, and action identity are not numerical fields. Negate and Evade are categorical operations and cannot be strengthened by increasing an amount.

Proposed initial numerical operation: signed additive adjustment only. Sum qualifying adjustments and clamp to the field's permitted range. This supplies increases and decreases without introducing multiplier order or rounding ambiguity. Turn cost has an explicit extension: persistent `TurnCostMultiplierEffect` rules multiply together at commitment. Other fields do not support multiplication, replacement, or priority overrides. Modify does not convert Row targeting into All targeting; numerical radius or target count can change only where supported.

Modification filters default to rules owned by the operation's target. A source-scoped filter instead reads rules on the action's source and can require all specified action flags. A source damage modifier that adds a typed damage amount creates that damage entry when the attack has no baseline entry of that type; it applies once per recipient and attack index, including each strike of a multi-weapon attack.

### Consume, Grant, Remove, and Transfer

Consume reduces a quantity without treating it as Health damage. Proposed default for effects: consume up to the available amount and record the actual reduction. A commitment cost instead requires the entire amount to be available before the action is accepted.

Grant increases Focus or another declared resource, adds an item, or adds a capability. Apply is the exclusive verb for statuses, and Restore is the exclusive verb for Health recovery. Grant must follow existing capacity and identity rules. It cannot implicitly equip an item or learn an entire progression branch.

Remove removes a referenced entity or association, rather than subtracting an arbitrary resource amount. Removing an equipment association unequips the item without destroying it; removing the item itself removes it from ownership and clears any equipment reference. The authored subject must distinguish those cases. Dispel handles statuses, and Consume handles quantities.

Transfer preserves what is transferred: the origin loses exactly what the destination receives. Validate both sides before mutation; if the requested transfer is invalid, neither side changes. Item identity is preserved and the item is unequipped at the origin when necessary. The destination does not automatically equip it. Initial resource transfers require the full specified amount. Health transfer is outside this operation's initial scope; life drain can explicitly compose Damage and Restore with an outcome dependency if needed.

### Apply and Dispel

Apply attaches a StatusEffectId and records runtime information needed by that status definition: source, duration, remaining uses, and parameters where applicable. The status definition specifies any triggered or resolution behavior using the same operation vocabulary.

Every status definition declares duplicate handling: reject, refresh, replace, or stack. Reject authoring that omits this rule when duplicates are possible. Duration must name its clock and boundary, such as expiration at the start of the recipient's next Turn. Avoid an ambiguous duration of “two turns.” A next-hit rule must declare which qualifying event consumes its use, including whether an evaded action counts.

Dispel removes statuses selected by identifier or explicit metadata filters. An optional whole-turn DurationReduction shortens matching timed statuses instead of removing them outright; exhausted durations expire immediately, while permanent and use-only lifetimes remain unchanged. Cleanse selects the negative tag and reduces duration by one turn. If a limit selects fewer than all matches, declare a stable selection order. Removing a status ends its future influence but does not reverse previous Damage, Restore, or movement. Natural expiration is lifecycle processing and does not require a character to perform a Dispel action.

Prevent is a persistent rule on a status, equipment, or innate rule collection. It blocks only the matching StatusEffectId from being applied to that recipient; other statuses and effects continue normally. Removing or expiring the rule immediately permits future applications again.

## Action composition and persistent behavior

An action defines an ordered collection of effects. Resolve them deterministically in authored order; later effects observe earlier resulting state. An unavailable operation produces an explicit no-change outcome and does not cancel unrelated effects unless the definition declares a dependency.

Persistent resistance, negation, prevention, evasion, restriction, or modification is represented by Apply installing a Status Effect whose definition supplies the corresponding behavior. Equipment or other existing state may also supply such rules. Do not add separate booleans such as IsSilenced for every status.

Target selection supplies recipients independently of the verb. Single target, row, column, adjacency, and all are targeting definitions, not additional effect verbs. For the initial model, determine an area's recipients once at the start of resolution. A Move within that action does not silently recalculate the other recipients. A definition requiring later target selection must state it explicitly.

Costs are commitment requirements, even when their accounting resembles Consume. Once a valid action is committed, its declared costs remain paid if its effects are evaded or negated. Validation failure before commitment pays no cost. Restriction checks, source attribution, and cost handling must use current runtime state.

## Data model boundaries

CharacterData uses a single `Actions` collection for character capabilities; separate `Attacks` and general `Actions` collections are not used.

Treat CharacterData as authored initial data. Mutable combat state holds current Health and Focus, item ownership and quantities, equipment associations, available capabilities/actions, active status instances, formation position for non-player characters, and next scheduled Turn. Reuse existing runtime ownership where these already exist rather than duplicating them in a new effects subsystem.

Initial StatusEffectId entries must be expanded into runtime instances using their definitions. Plain identifiers alone cannot hold independent source, expiration, or use counts. `CharacterData.Actions` is the single authored character repertoire; inspect `ActionData` and the scheduler for execution details.

## Resolution outcomes

Return enough structured information for presentation and verification: committed action and source, recipient, effect identity and verb, requested values, actual changes, relevant rule attribution, and a result such as applied, reduced, negated, evaded, or unchanged with a reason. These are resolution results, not new gameplay phases or a second action vocabulary.

The same starting state and committed action must produce the same outcome and final state. Definitions must reject unsupported fields, unresolved identifiers, ambiguous duration rules, negative base quantities, and invalid recipient kinds before gameplay where possible.

## Acceptance examples

| Case | Required result |
|---|---|
| Incoming 1 Basic + 2 Slashing + 1 Fire; Resist Slashing by 1 | Three damage before Health clamping. |
| Same profile; Negate Fire | Three damage; non-damage effects still resolve. |
| Damage + Delay + Apply against a recipient with qualifying Evade | None of those effects applies to that recipient; committed costs remain paid. |
| Area action; one recipient evades | Remaining recipients resolve the complete action normally. |
| Two Slashing entries of 1; Resist Slashing by 1 | One total Slashing damage, not zero. |
| Delay 2 against next Turn 10 | Next Turn becomes 12 in existing scheduler units. |
| Restrict weapon actions | Matching actions become unavailable; equipment remains owned and equipped. |
| Remove an equipment association | Item becomes unequipped and stays in inventory. |
| Modify Damage by +2, then qualifying Negate | Zero matching damage. |
| Consume Focus 3 when only 2 remains | Effect consumes 2; a cost of 3 would reject commitment instead. |
| Transfer item to an invalid destination | No ownership or equipment changes at either endpoint. |
| Apply resistance status, then Dispel it | Future damage loses that resistance; earlier damage is unchanged. |
| Move to an occupied position without a displacement rule | Formation remains unchanged; other independent effects still resolve. |

## Implementation scope

Implement the operation definitions, typed validation, deterministic resolution, runtime status behavior, and structured outcomes. Verify the acceptance examples with focused resolver tests. Keep balance values, ability names, Focus recovery, enemy intent, reactions, story branching, and new UI design outside this change. The proposed defaults above should remain visible in code and authoring documentation so future extensions cannot change their meanings accidentally.
