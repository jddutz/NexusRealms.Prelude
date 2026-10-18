# Damage Types

Damage is an ordered set of nonnegative, integer-valued points with explicit types. A hit can contain several types: a normal sword strike, for example, deals 1 Basic and 2 Slashing damage before modifiers and mitigation.

This document describes the current runtime rules. Ability descriptions supply their own amounts and targeting; the damage type alone does not add status effects or action restrictions.

## Types and categories

| Damage type | Category | Current use |
| --- | --- | --- |
| Basic | None | Unarmed strikes, the Basic component of catalog melee weapons, and explicit ability bonuses. |
| Slashing | Physical | Cutting weapons and effects such as Sandstorm. |
| Piercing | Physical | Piercing weapons, ranged weapons, and some thrown weapons. |
| Crushing | Physical | Blunt weapons, rocks, and effects such as Twister. |
| Fire | Elemental | Explicit Fire damage effects. |
| Ice | Elemental | Explicit Ice damage effects. |
| Lightning | Elemental | Electrical damage, including Hurricane's Lightning component. |
| Poison | None | Explicit Poison damage effects. |

`DamageCategory` also defines Spiritual, Mental, and Magical, but no current `DamageType` maps to those categories. They do not currently match any typed damage.

Basic and Poison are deliberately outside Physical and Elemental. Physical resistance does not reduce either type. Basic is not universally unmitigable: a rule that explicitly resists or negates Basic can still affect it.

Damage categories and action flags are separate. `ActionFlags.Magical` controls whether an action is magical for restrictions such as Silence; it does not put that action's damage in the Magical category. Likewise, a Melee action can deal several damage types.

## Weapon damage

The following are the catalog's ordinary strike profiles, before Sharp, status modifiers, or mitigation:

| Weapon | Damage |
| --- | --- |
| Unarmed | 1 Basic |
| Sword | 1 Basic + 2 Slashing |
| Axe | 1 Basic + 1 Slashing + 1 Crushing |
| Knives | 1 Basic + 1 Piercing + 1 Slashing |
| Clubs | 1 Basic + 2 Crushing |
| Maces / Flail | 1 Basic + 1 Crushing + 1 Piercing |
| Rock | 1 Basic + 1 Crushing |
| Pistol / Bow / Crossbow | 3 Piercing |
| Sling | 2 Piercing |

Sharp adds 1 Slashing only when the weapon already has a positive Slashing component. It does not add a new Slashing component to a weapon without one.

Thrown profiles can differ from ordinary strikes: the catalog defines thrown Sword as 1 Piercing, thrown Knives as 2 Piercing, and thrown Rock as 2 Crushing.

For ability attacks built by `MeleeWeaponAttacks.Each` (Dual Strike, Fury, Rampage, and Whirlwind), each distinct equipped melee weapon gets its own attack index. With multiple melee weapons, the helper omits the Basic component from each weapon strike. With one weapon, it preserves Basic. These abilities therefore do not simply multiply the ordinary single-weapon profile by the number of weapons.

## Resolution and mitigation

The shared action resolver processes effects in order:

1. Targeting and reach determine eligible recipients. Evasion can cancel incoming target effects before damage resolution.
2. Matching numerical modifiers adjust each damage amount; the result cannot be negative.
3. A matching Negate rule sets that component to zero.
4. Otherwise, matching Resist rules reduce the component using their remaining resistance budget.
5. Remaining damage reduces Health, stopping at zero.

Resistance budgets are tracked separately for each recipient, resistance rule, and attack index within an action. By default, each damage type gets its own budget. `ShareAcrossTypes: true` shares one budget across matching types in that attack. Distinct weapon attacks get separate budgets.

For example, Brace grants 1 shared Physical resistance per incoming attack:

- A sword's 1 Basic + 2 Slashing becomes 1 Basic + 1 Slashing: 2 damage total.
- An axe's 1 Basic + 1 Slashing + 1 Crushing loses one Physical point in total, rather than one point from each Physical type: 2 damage total.
- Basic, Poison, and Elemental components are unaffected by Brace.

Cooling Breeze negates Fire specifically; it does not negate every Elemental type. Negation removes the matching damage component, not unrelated damage or other effects in the action.

## Abilities and passive triggers

- **Onslaught** adds 1 Basic damage per Melee attack for two timeline turns. The resolver can insert a Basic component when that attack has none, including multi-weapon attacks whose ordinary Basic component was omitted.
- **Gigas** stacks independent three-turn effects that each double Crushing Melee damage. A status expires on its absolute timeline boundary, so earlier stacks may expire while later Gigas uses are being committed.
- **Primal Rage** grants 1 Focus per point of Basic Health damage actually taken. Mitigated damage and overkill beyond remaining Health do not generate Focus.
- **Bloodlust** grants its living owner 1 Focus when an opposing character is defeated, independently of damage type.
- **Shock** removes Focus using a Consume effect. It deals no Health damage and does not trigger Primal Rage.
- Fear, Berserk, delays, and row movement are separate effects, not damage types.

Resolved damage and actual Health loss are distinct: a 5-damage effect against a target with 2 Health can retain a resolved amount of 5 while changing Health by only 2. Passive damage triggers use the actual Health loss.

AI previews use the same action resolver on an isolated combat state, so they include these damage modifiers, mitigation rules, and passive triggers.

## Implementation references

- [Damage types and category mapping](../Prelude/DataModel/DamageType.cs)
- [Damage profiles and Sharp](../Prelude/DataModel/DamageProfile.cs)
- [Weapon catalog](../Prelude/Combat/WeaponCatalog.cs)
- [Multi-weapon attack construction](../Prelude/Actions/MeleeWeaponAttacks.cs)
- [Action resolution and mitigation](../Prelude/Actions/CombatActionResolver.cs)
- [Health damage application](../Prelude/Combat/DamageReceiver.cs)
- [Status definitions](../Prelude/Database/StatusEffectsDB.cs)
- [Action effects](Action-Effects.md)
