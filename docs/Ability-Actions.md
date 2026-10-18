# Ability actions

Ability definitions stay with their Zodiac graph, such as `Database/Progression/Aquarius.cs`.
Each `ProgressionGraph` owns its ability definitions and placements; `Storyline.GetAbilities`
resolves definitions within the selected Zodiac, so ability IDs can be reused by other graphs.
`PlaceholderAbilities.cs` supplies the temporary definitions used by the unimplemented graphs.

Each data-only `AbilityData` references exactly one implementation through `ActionId`.
It supplies the name, icon, Turn cost, Focus cost and `TargetingProfile`.
Current costs default to 1 Turn and 0 Focus until explicitly balanced.

`AbilityActionRegistry` maps action identities to runtime `AbilityAction` implementations.
Resolving an ability binds its authored data to a copy of the implementation. Implementations
have no intrinsic AbilityId back-reference; progression requirements are added when binding
the runtime capability. Learning registers that capability and its HUD command.

To implement an action, derive a record from `AbilityAction`, supply its stable ActionId,
and implement `Execute()` to return ordered typed effects. Override `SelectTargets()` when
the shared TargetingProfile selection is insufficient. These methods must be pure so the
same rules support validation, preview and execution. Replace the corresponding scaffold
entry in the registry; keep behavior out of the database.

`DowseAction` is the first implemented example: its `Execute()` supplies a source-scoped
`GrantEffect` for one Focus. Aquarius authors Self targeting, 1 Turn and 0 Focus cost.
The gain happens at resolution; the timeline still advances by its authored Turn cost.

The resolver applies effects and pays costs through the existing combat system. Registered
scaffolds expose no effects and remain unavailable in player combat and AI selection until
replaced with implementations. Every progression graph owns the ability definitions for its
nodes, and its action mappings are validated independently of other Zodiacs.

Implemented support actions include Heal (+3 Health with no gameplay maximum), Purge
(all statuses from one target), and Cleanse (one turn from every timed status tagged
`negative`). Heal and Cleanse affect only the caster without target selection; Purge can select either team.
All currently use the provisional 1 Turn / 0 Focus costs authored in Aquarius.
Cleanse handles both timeline and recipient-turn clocks, removes statuses whose duration
is exhausted, and leaves positive, permanent and use-only lifetimes unchanged. Shortened
timeline expirations are rescheduled through the simulation-safe scheduler.

Defiance is self-only and uses Aries' authored 0.5 Turn / 2 Focus costs. It removes every
status tagged `negative`, then applies Berserk for one timeline turn per removed status.
The duration is planned from the caster's status snapshot, so previews and committed actions
agree; with no negative statuses, it does not apply a zero-duration Berserk.

Recovery is self-only, costs 1 Focus, and converts all Focus available before commitment
into 2 Health per Focus. The activation cost is included in the restoration amount; any
remaining Focus is consumed by a source-scoped effect. Preview and commitment use the same
post-cost Focus snapshot.

King Maker is a single melee strike with the primary equipped weapon (or an unarmed strike),
then adds Basic damage and consumes all remaining Focus. Bonus damage for 0-8 Focus is
1, 2, 3, 5, 8, 13, 19, 26 and 34. Starting at 9 Focus, each value is the sum of the prior
two values; the amount saturates at `Int32.MaxValue`.

Fury attacks with each equipped melee weapon (or uses an unarmed strike if none are equipped),
then applies a stackable 25% Turn-cost reduction to its caster for the caster's next turn.
Multiple Fury stacks multiply their Turn-cost modifiers; the speed status is applied after
the attacks and expires at the start of the caster's following turn.

Gale shifts all living enemies one row backward; Tailwind shifts them one row forward.
Both use all-opponent targeting and the provisional 1 Turn / 0 Focus costs. Their typed
ShiftRowEffect resolves the leading row first, then left to right, so followers can use
vacated slots. Each recipient moves at most once. Within the adjacent row, choose the
nearest free slot by the shared staggered formation coordinates; equal distances choose
the lower slot enum value. Full rows and formation edges prevent movement. No swapping,
row skipping, damage, or displacement of occupants occurs. Evasion follows ordinary
target-effect rules, and successful shifts update the scheduler and presentation events.

Cooling Breeze applies the positive cooling-breeze status only to the caster,
without target selection. Its Negate rule blocks only Fire damage for three timeline turns after
resolution. Recasting refreshes the expiration without stacking. It is removable by
Purge and unaffected by Cleanse. The ability currently costs 1 Turn and 0 Focus.

Levitate is self-only and costs 1 Focus plus the provisional 1 Turn. After payment,
each remaining Focus grants one timeline turn of the positive Elevated status (+1 Elevation).
With 4 Focus, casting leaves 3 Focus and grants 3 turns of elevation. Levitate requires at least 2 Focus before commitment and remains disabled below that threshold;
its actual cost is still only 1 Focus. Recasting refreshes without stacking.
Purge and natural expiration return the recipient to the ground; Cleanse leaves it alone.

StatusEffectData.Elevation declares elevation independent of status names. The resolver,
weapon command targeting and AI all exclude elevated recipients from short-range melee.
Weapon reach comes from equipment; typed actions declare MeleeRange. Ranged, thrown and
longer melee attacks remain eligible under their ordinary targeting rules. Elevated enemies
keep their formation slots and continue occupying their rows.

Actions and individual ActionEffects can declare RequiresGroundContact. Elevated recipients
negate those operations, leaving unrelated effects untouched. Future Flood and Earthquake
implementations must declare this metadata explicitly; names and damage types imply no
ground-contact rule. Focus-scaled ApplyEffect durations are computed in the shared resolver
after commitment costs, keeping simulation and live combat consistent.

Pressure Shield is self-only. Like Levitate, it requires at least 2 Focus, costs 1 Focus,
and lasts one timeline turn per remaining Focus. It is a positive, refreshable status.
It reduces total Physical damage by 1 per incoming attack, sharing its reduction across
Physical types in that attack. Basic has no category and is unaffected; the existing
Physical category includes Slashing, Piercing, Crushing and Poison.

Whirlwind attacks all living front-row enemies once per equipped melee weapon, without
target selection. One melee weapon uses full normal damage. With two melee weapons,
remove Basic damage from each weapon attack and retain all other typed damage unchanged.
Ranged weapons and duplicate references to a single two-handed weapon do not add attacks.
Each weapon attack has an independent resistance budget and obeys that weapon's melee
reach against Elevated enemies. Damage is planned from current equipment at resolution,
including in previews. No melee weapon means Whirlwind is unavailable.

Bewilder targets one enemy, costs 0.5 Turn and 0 Focus, and applies the negative
Bewildered status for two timeline turns. It restricts actions that spend Focus through
commitment costs or voluntary source-scoped Consume/Transfer operations. Existing Focus
is preserved; free actions, Focus gains and involuntary drains remain unaffected. The
same restriction covers HUD availability, legacy commands, commitment and AI candidates.
Recasting refreshes without stacking. Cleanse exhausts its one-turn duration and Purge
removes it outright.

Every living character gains 1 Focus at whole timeline boundaries (1, 2, 3, ...),
independent of actor turns or fractional action costs. These events are copied into
simulation state without duplication. Eye of the Storm is self-only and adds four
caster-only bonuses: +1, +2, +4 and +7 at the third through sixth upcoming boundaries.
The caster's total gains are therefore 1, 1, 2, 3, 5, 8; everyone else still gains 1.
The positive status ends after the last bonus, Purge cancels outstanding bonuses, and
refreshing it invalidates old events and restarts the sequence. Normal regeneration
continues afterward. Current Eye costs are the provisional 1 Turn / 0 Focus.

TargetingProfile.Flags is a [Flags] enum. None declares no requirements or affinities;
Visual requires sight and Magical declares magical affinity. The flags are copied from AbilityData to its runtime
action and preserved through HUD, legacy command and AI adapters. Ordinary hand strikes,
weapon attacks, throws and Bewilder are explicitly Visual. Self and untargeted area
spells carry Magical without Visual; flags are authored rather than inferred from shape.

Blind is a negative, refreshable status that prevents actions with the Visual flag.
It changes availability and commitment, not hit chance, and gives no damage immunity.
Fog applies Blind to all enemies. Sandstorm deals 1 Slashing damage to each enemy and
independently applies Blind, including when that damage is resisted. Both are nonvisual
all-enemy actions, cost the provisional 1 Turn / 0 Focus, and apply Blind for 2 timeline
turns. Recasting refreshes without stacking; Cleanse shortens Blind and Purge removes it.

Slow and Haste share persistent Turn-cost multipliers (defaults 1.5x and 0.75x). Active multipliers multiply together, and the cost is fixed when an action is committed. Existing queued turns, status durations, and Focus regeneration retain their timeline timing. Wait still schedules just after the next character. Status applications choose their duration.

Headwind applies the shared Slow status (1.5x Turn costs) to one Visual enemy target for 3 timeline turns. It retains its authored 1 Turn / 0 Focus costs and does not shift an already queued enemy turn.

Fixed-duration defaults allow time to act after casting: Slow/Haste, Headwind, Cooling Breeze and planned Wind Shear last 3 timeline turns; Blind, Bewilder and planned Silence last 2. Focus-based Levitate and Pressure Shield durations remain unchanged.

Hurricane deals 2 Basic and 1 Electrical (`Lightning`) damage to every enemy across all rows. Its all-enemy targeting is nonvisual, costs 1 Turn and 0 Focus, and uses normal per-recipient damage mitigation and evasion.

Silence applies a negative, refreshable Silenced status to one Visual enemy target for 2 timeline turns (1 Turn / 0 Focus). It blocks the Magical targeting flag, representing magical affinity regardless of speech or Focus cost. Visual and Magical combine independently. Aquarius spells carry Magical; Whirlwind and ordinary weapon attacks do not. Cleanse shortens Silence and Purge removes it.

Sound Barrier applies the same Silenced status as Silence to all living enemies in the back row for 2 timeline turns. It costs 1 Turn / 0 Focus and requires no selected or Visual target. It has Magical affinity, so Silence prevents its use. Recasting refreshes the shared status without stacking.

Twister deals 1 Crushing damage and independently delays each enemy's next scheduled turn by 0.5 turns. It affects all enemy rows, costs 1 Turn / 0 Focus, and is Magical without Visual targeting. Resistance can absorb damage without preventing Delay; evasion protects against both effects.

Wind Shear applies Slow (1.5x Turn costs) to every living enemy in the front row for 3 turns, costing 1 Turn / 0 Focus. It is Magical without Visual targeting and shares Headwind's refreshable Slow status. Queued turns are unchanged.
