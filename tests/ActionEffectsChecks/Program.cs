using NexusRealms.Prelude.Rules;
using System.Collections.Immutable;
using NexusRealms.Prelude;
using NexusRealms.Prelude.Actions;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static void Reject(Action operation, string message)
{
    try { operation(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; }
    throw new InvalidOperationException(message);
}
static StatusEffectData Status(string id, params ActionEffect[] rules) => new()
{
    Id = id, Name = id, Description = id, Icon = new("unused"),
    Duplicates = DuplicateStatusRule.Refresh, Rules = rules.ToImmutableArray(),
};
var resistance = Status("resistance", new ResistEffect(new(DamageType.Slashing), 1));
var negate = Status("negate", new NegateEffect(new(DamageType.Fire)));
var prevention = Status("prevention", new PreventEffect("resistance"));
var evade = Status("evade", new EvadeEffect(new()));
var restriction = Status("restriction", new RestrictEffect(new(Classification: "weapon")));
var modifier = Status("modifier", new ModifyEffect(new(EffectVerb.Damage), NumericalField.DamageAmount, 2));
var definitions = new[] { resistance, negate, prevention, evade, restriction, modifier,
    Status("reject") with { Duplicates = DuplicateStatusRule.Reject },
    Status("replace") with { Duplicates = DuplicateStatusRule.Replace },
    Status("stack") with { Duplicates = DuplicateStatusRule.Stack },
    Status("negative-long") with { Tags = ["negative"] },
    Status("negative-short") with { Tags = ["negative"] },
    Status("negative-clock") with { Tags = ["negative"] },
    Status("negative-permanent") with { Tags = ["negative"] },
    Status("positive") with { Tags = ["positive"] },
    NexusRealms.Prelude.Database.StatusEffects.CoolingBreeze,
    NexusRealms.Prelude.Database.StatusEffects.Elevated,
    NexusRealms.Prelude.Database.StatusEffects.PressureShield,
    NexusRealms.Prelude.Database.StatusEffects.Bewildered,
    NexusRealms.Prelude.Database.StatusEffects.EyeOfTheStorm,
    NexusRealms.Prelude.Database.StatusEffects.Slow,
    NexusRealms.Prelude.Database.StatusEffects.Haste,
    NexusRealms.Prelude.Database.StatusEffects.Fury,
    NexusRealms.Prelude.Database.StatusEffects.Onslaught,
    NexusRealms.Prelude.Database.StatusEffects.Silenced,
    NexusRealms.Prelude.Database.StatusEffects.Blind,
    NexusRealms.Prelude.Database.StatusEffects.Berserk }.ToDictionary(status => status.Id);

Fixture New() => new(definitions);
void Apply(Fixture f, string id, string recipient = "enemy", StatusPersistence? persistence = null)
    => f.Resolve(recipient, new ApplyEffect(id, persistence ?? new()));

var aiFixture = New(); Apply(aiFixture, "resistance");
var mitigated = new ExampleAction("a-mitigated", new DamageEffect(DamageType.Slashing, 6));
var effective = new ExampleAction("b-effective", new DamageEffect(DamageType.Basic, 6))
    { Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] };
aiFixture.Player.Loadout.Actions[mitigated.Id] = mitigated;
aiFixture.Player.Loadout.Actions[effective.Id] = effective;
var preview = aiFixture.Resolver.Preview("player", effective, "enemy");
Check(aiFixture.Enemy.Health.Health == 20 && aiFixture.Player.Loadout.Focus == 2 && preview.Characters["enemy"].Health == 14
    && preview.Characters["player"].Focus == 1, "Preview must isolate health and costs.");
var decision = CombatDecisionMaker.Decide(aiFixture.Resolver, "player", new());
Check(decision.Selected?.Action.Id == effective.Id, "Immediate policy must compare resolved damage.");
Check(CombatDecisionMaker.Decide(aiFixture.Resolver, "player", new()).Selected == decision.Selected, "Repeated decisions must be stable.");
aiFixture.Resolver.Commit("player", effective, "enemy");
Check(aiFixture.Enemy.Health.Health == preview.Characters["enemy"].Health && aiFixture.Player.Loadout.Focus == preview.Characters["player"].Focus + 1,
    "Immediate preview must match committed health; live recovery crosses one Focus-regeneration boundary.");
Console.WriteLine("AI preview and deterministic selection checks passed.");
var mixed = new ActionEffect[] { new DamageEffect(DamageType.Basic, 1), new DamageEffect(DamageType.Slashing, 2), new DamageEffect(DamageType.Fire, 1) };
var f = New(); Apply(f, "resistance");
var outcome = f.Resolve("enemy", mixed);
Check(f.Enemy.Health.Health == 17 && outcome.Effects.Sum(effect => effect.ResolvedAmount) == 3, "Typed resistance acceptance case.");
f = New(); Apply(f, "prevention", "enemy");
outcome = f.Resolve("enemy", new ApplyEffect("resistance", new()));
Check(f.Enemy.Statuses.Count == 1 && f.Enemy.Statuses.Single().Definition.Id == "prevention"
    && outcome.Effects.Single().Result == EffectResult.Unchanged && outcome.Effects.Single().Rules.Length == 1,
    "Prevent must block the matching status application and attribute its persistent rule.");
outcome = f.Resolve("enemy", new ApplyEffect("negate", new()));
Check(outcome.Effects.Single().ActualChange == 1 && f.Enemy.Statuses.Count == 2,
    "Prevent must not block applications of other statuses.");
f = New(); Apply(f, "negate");
outcome = f.Resolve("enemy", mixed.Concat([new DelayEffect(2)]).ToArray());
Check(f.Enemy.Health.Health == 17 && f.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 12,
    "Negation must preserve nonmatching damage and non-damage effects.");
f = New(); Apply(f, "evade");
var costly = new ExampleAction("costly", new DamageEffect(DamageType.Basic, 3), new DelayEffect(2), new ApplyEffect("resistance", new()))
    { Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] };
f.Player.Loadout.Actions[costly.Id] = costly; f.Resolver.Register(costly);
outcome = f.Resolver.Commit("player", costly, "enemy");
Check(outcome.Effects.All(effect => effect.Result == EffectResult.Evaded) && f.Player.Loadout.Focus == 2
    && f.Enemy.Health.Health == 20 && f.Enemy.Statuses.Count == 1, "Evasion must cancel every target effect and retain costs.");
f = New(); Apply(f, "evade");
var otherHealth = new DamageReceiver(20);
f.Combat.Add(new("other", false, 10, team: CombatTeam.Enemies));
f.Resolver.Add(new("other", false, CombatTeam.Enemies, otherHealth, new(), FormationSlot.FrontRight));
var area = new ExampleAction("area", new DamageEffect(DamageType.Basic, 3), new DelayEffect(2))
    { Targeting = new(TargetShape.All) };
f.Player.Loadout.Actions[area.Id] = area; f.Resolver.Register(area);
outcome = f.Resolver.Commit("player", area, null);
Check(f.Enemy.Health.Health == 20 && otherHealth.Health == 17 && outcome.Recipients.Length == 2, "Area evasion must be recipient-specific.");
f = New(); Apply(f, "resistance");
outcome = f.Resolve("enemy", new DamageEffect(DamageType.Slashing, 1), new DamageEffect(DamageType.Slashing, 1));
Check(f.Enemy.Health.Health == 19 && outcome.Effects.Sum(effect => effect.ResolvedAmount) == 1, "Resistance must not multiply for split same-type entries.");
f.Resolve("enemy", new DelayEffect(2));
Check(f.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 12, "Delay must shift the scheduled turn by existing units.");
Check(f.Resolve("player", new DelayEffect(2)).Effects.Single().Result == EffectResult.Unchanged, "Delay must not affect an active committed turn.");
f = New(); Apply(f, "restriction", "player");
var weapon = new ExampleAction("weapon", new DamageEffect(DamageType.Basic, 1)) { Classifications = ["weapon"] };
f.Player.Loadout.Actions[weapon.Id] = weapon; f.Resolver.Register(weapon);
Check(!f.Resolver.CanCommit("player", weapon, "enemy") && !f.Player.Loadout.Available(CombatLoadout.RightHandStrike)
    && f.Player.Loadout.Equipment.ContainsKey("Right hand"), "Restriction must affect new and legacy actions without removing equipment.");
var ownedBeforeUnequip = f.Player.Loadout.Items.ToArray();
f.Resolve("player", new RemoveEffect(new RemoveEquipment("Right hand")));
Check(f.Player.Loadout.Items.SequenceEqual(ownedBeforeUnequip) && f.Player.Loadout.Equipment.Count == 0
    && f.Player.Loadout.Inventory.Count(item => item is not null) == ownedBeforeUnequip.Length, "Unequip must preserve ownership.");
f = New(); Apply(f, "modifier"); Apply(f, "negate");
outcome = f.Resolve("enemy", new DamageEffect(DamageType.Fire, 1));
Check(f.Enemy.Health.Health == 20 && outcome.Effects.Single().Result == EffectResult.Negated, "Negate must override numerical damage changes.");
f = New();
outcome = f.Resolve("player", new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 3));
Check(f.Player.Loadout.Focus == 0 && outcome.Effects.Single().ActualChange == 2, "Effect consumption may partially apply.");
f = New();
var expensive = costly with { Id = "expensive", Costs = [new(new ResourceSubject(ResourceKind.Focus), 3)] };
f.Player.Loadout.Actions[expensive.Id] = expensive; f.Resolver.Register(expensive);
Check(!f.Resolver.CanCommit("player", expensive, "enemy") && f.Player.Loadout.Focus == 2, "Full commitment costs must reject before mutation.");
var sword = f.Player.Loadout.HeldItem(StrikeHand.Right)!;
for (var index = 0; index < CombatLoadout.InventoryCapacity; index++)
    Check(f.Enemy.Loadout.TryAddItem(new($"full-{index}", "Item", "Accessory")), "Fill destination inventory.");
outcome = f.Resolve("player", new TransferEffect("enemy", new ItemQuantitySubject(sword.Id), 1));
Check(outcome.Effects.Single().ActualChange == 0 && f.Player.Loadout.Equipment["Right hand"] == sword.Id.Value
    && f.Player.Loadout.Items.Contains(sword) && !f.Enemy.Loadout.Items.Any(item => item.Id == sword.Id), "Invalid transfer must be atomic.");
f = New(); sword = f.Player.Loadout.HeldItem(StrikeHand.Right)!;
f.Resolve("player", new TransferEffect("enemy", new ItemQuantitySubject(sword.Id), 1));
Check(!f.Player.Loadout.Items.Contains(sword) && f.Player.Loadout.Items.Single().Id.Value == "starting-rocks"
    && f.Player.Loadout.Equipment.Count == 0 && ReferenceEquals(f.Enemy.Loadout.Items.Single(), sword)
    && f.Enemy.Loadout.Equipment.Count == 0, "Valid item transfer preserves identity without equipping.");
f = New(); Apply(f, "resistance"); f.Resolve("enemy", new DamageEffect(DamageType.Slashing, 2));
f.Resolve("enemy", new DispelEffect(new("resistance"))); f.Resolve("enemy", new DamageEffect(DamageType.Slashing, 2));
Check(f.Enemy.Health.Health == 17 && f.Enemy.Statuses.Count == 0, "Dispel ends future influence without reversing past damage.");
f = New();
f.Combat.Add(new("occupant", false, 10, team: CombatTeam.Enemies));
f.Resolver.Add(new("occupant", false, CombatTeam.Enemies, new(20), new(), FormationSlot.BackLeft));
f.Enemy.Health.Health = 10;
outcome = f.Resolve("enemy", new MoveEffect(FormationSlot.BackLeft), new RestoreEffect(1));
Check(f.Enemy.Position == FormationSlot.FrontLeft && f.Enemy.Health.Health == 11 && outcome.Effects[0].Result == EffectResult.Unchanged, "Occupied move must not cancel unrelated operations.");
f.Resolve("enemy", new MoveEffect(FormationSlot.MiddleLeft));
Check(f.Enemy.Position == FormationSlot.MiddleLeft && f.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Row == CombatRow.Middle, "Move must update formation and scheduler row.");
f = New(); f.Enemy.Health.Health = 1;
outcome = f.Resolve("enemy", new DamageEffect(DamageType.Basic, 3));
Check(outcome.Effects.Single() is { RequestedAmount: 3, ResolvedAmount: 3, ActualChange: 1 }, "Attempted, resolved, and actual damage must remain distinct.");
f.Resolve("enemy", new RestoreEffect(10));
Check(f.Enemy.Health.Health == 0, "Restore alone must not resurrect.");
f = New(); Apply(f, "reject"); Apply(f, "reject"); Check(f.Enemy.Statuses.Count == 1, "Reject duplicate policy.");
Apply(f, "stack"); Apply(f, "stack"); Check(f.Enemy.Statuses.Count(status => status.Definition.Id.Value == "stack") == 2, "Stack policy.");
Apply(f, "replace"); var sequence = f.Enemy.Statuses.Last().Sequence; Apply(f, "replace");
Check(f.Enemy.Statuses.Last().Sequence > sequence, "Replace creates a new application order.");
Apply(f, "resistance"); sequence = f.Enemy.Statuses.Last().Sequence; Apply(f, "resistance");
Check(f.Enemy.Statuses.Last().Sequence == sequence, "Refresh preserves application order.");
f = New(); Apply(f, "resistance", "player", new(StatusClock.RecipientTurnStart, 1));
f.Combat.SubmitAction(new(1, (_, _) => { }));
Check(f.Player.Statuses.Count == 0, "Next-turn expiration must precede action selection.");
f = New(); Apply(f, "resistance", "enemy", new(StatusClock.Timeline, 1));
f.Combat.SubmitAction(new(1, (_, _) => { }));
Check(f.Enemy.Statuses.Count == 0, "Timeline expiration must occur at its explicit boundary.");
f = New(); Apply(f, "evade", "enemy", new(Uses: 1, UseEvent: StatusUseEvent.IncomingAction));
f.Resolve("enemy", new DamageEffect(DamageType.Basic, 3));
Check(f.Enemy.Statuses.Count == 0 && f.Enemy.Health.Health == 20, "Declared incoming-action use includes an evaded action.");
f = New(); Apply(f, "evade", "enemy", new(Uses: 1, UseEvent: StatusUseEvent.NonEvadedIncomingAction));
f.Resolve("enemy", new DamageEffect(DamageType.Basic, 3));
Check(f.Enemy.Statuses.Single().RemainingUses == 1, "Declared non-evaded use must exclude evasion.");
f = New();
var unlock = new TestAbilityAction("unlocked")
    { Requirements = new() { Equipment = [new("Right hand")] } };
var ability = new AbilityData { Id = "progression", Name = "Progression", Description = "Test", Icon = new("unused"), ActionId = new(unlock.Id) };
var abilityRegistry = new AbilityActionRegistry([unlock]);
f.Player.Loadout.Learn(ability, abilityRegistry); var learned = f.Player.Loadout.Actions[unlock.Id]; f.Resolver.Register(learned);
Check(f.Resolver.CanCommit("player", learned, "enemy"), "Ability and equipped item unlock an action.");
f.Player.Loadout.Learn(ability);
Check(f.Player.Loadout.Commands.Count(command => command.Id.Value == unlock.Id) == 1
    && f.Player.Loadout.Actions[unlock.Id].Requirements.Abilities.Count(id => id == ability.Id) == 1,
    "Repeated learning must not duplicate commands or prerequisites.");
var conflictingAbility = ability with { Id = "conflicting" };
Reject(() => f.Player.Loadout.Learn(conflictingAbility, abilityRegistry), "Conflicting unlocks must be rejected.");
Check(!f.Player.Loadout.LearnedAbilities.Contains(conflictingAbility.Id), "Rejected learning must not mutate progression.");
var committedUnlock = f.Resolver.Commit("player", learned, "enemy");
Check(f.Enemy.Health.Health == 19 && committedUnlock.Action.Id == unlock.Id
    && committedUnlock.Effects.Single().ActualChange == 1, "Learned capability must resolve through shared combat effects.");
f.Resolve("player", new RemoveEffect(new RemoveEquipment("Right hand")));
Check(!f.Resolver.CanCommit("player", learned, "enemy") && f.Player.Loadout.LearnedAbilities.Contains(ability.Id), "Equipment loss disables action without unlearning progression.");
Reject(() => f.Resolver.Register(new ExampleAction("negative", new DamageEffect(DamageType.Basic, -1))), "Negative effect validation.");
Reject(() => f.Resolver.Register(new ExampleAction("missing", new ApplyEffect("unknown", new()))), "Unresolved status validation.");
Reject(() => CombatRules.Persistence(new(StatusClock.RecipientTurnStart, 1.5f)), "Ambiguous duration validation.");
Reject(() => CombatRules.ValidateStatus(Status("invalid", new ModifyEffect(new(EffectVerb.Negate), NumericalField.DamageAmount, 2))), "Unsupported numerical field validation.");
Reject(() => CombatRules.ValidateStatus(Status("invalid-modifier-flags",
    new ModifyEffect(new(EffectVerb.Damage, DamageType.Basic, (ActionFlags)8, RecipientScope.Source), NumericalField.DamageAmount, 1))),
    "Unknown action flags in modifier filters must be rejected.");
Reject(() => CombatRules.ValidateStatus(Status("invalid-modifier-recipient",
    new ModifyEffect(new(EffectVerb.Damage, DamageType.Basic, Recipient: (RecipientScope)99), NumericalField.DamageAmount, 1))),
    "Unknown modifier recipient scopes must be rejected.");
f = New();
outcome = f.Resolve("player", new TransferEffect("enemy", new ResourceSubject(ResourceKind.Focus), 2));
Check(f.Player.Loadout.Focus == 0 && f.Enemy.Loadout.Focus == 2 && outcome.Effects.Single().ActualChange == 2, "Focus transfer must conserve the full amount.");
f.Resolve("player", new TransferEffect("enemy", new ResourceSubject(ResourceKind.Focus), 1));
Check(f.Enemy.Loadout.Focus == 2, "Unavailable transfer must not partially mutate either endpoint.");
f.Resolve("player", new GrantEffect(new GrantResource(ResourceKind.Focus, 3)));
Check(f.Player.Loadout.Focus == 3, "Grant must recover Focus without Health damage semantics.");
var capability = new ExampleAction("granted", new RestoreEffect(1)) { Targeting = new(TargetShape.Self) };
f.Resolver.Register(capability);
f.Resolve("player", new GrantEffect(new GrantCapability(capability.Id)));
Check(f.Player.Loadout.Actions.ContainsKey(capability.Id) && f.Player.Loadout.LearnedAbilities.Count == 0, "Grant capability must not learn progression.");
f.Resolve("player", new RemoveEffect(new RemoveCapability(capability.Id)));
Check(!f.Player.Loadout.Actions.ContainsKey(capability.Id) && f.Player.Loadout.Find(capability.Id) is null, "Remove capability must remove its selectable adapter.");
var invalidMove = new ExampleAction("player-move", new MoveEffect(FormationSlot.BackLeft))
    { Targeting = new(TargetShape.Self), Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] };
f.Player.Loadout.Actions[invalidMove.Id] = invalidMove;
Check(!f.Resolver.CanCommit("player", invalidMove, null) && f.Player.Loadout.Focus == 3, "Invalid recipient kinds must reject before paying costs.");
f = New(); Apply(f, "stack"); Apply(f, "reject");
outcome = f.Resolve("enemy", new DispelEffect(new(), 1));
Check(outcome.Effects.Single().ActualChange == 1 && f.Enemy.Statuses.Single().Definition.Id.Value == "reject", "Limited dispel must choose oldest application first.");
f = New();
f.Combat.Add(new("row-other", false, 10, team: CombatTeam.Enemies));
var rowOther = new DamageReceiver(20);
f.Resolver.Add(new("row-other", false, CombatTeam.Enemies, rowOther, new(), FormationSlot.FrontRight));
var frozen = new ExampleAction("frozen-row", new MoveEffect(FormationSlot.MiddleLeft), new DamageEffect(DamageType.Basic, 1))
    { Targeting = new(TargetShape.Row) };
f.Player.Loadout.Actions[frozen.Id] = frozen; f.Resolver.Register(frozen);
outcome = f.Resolver.Commit("player", frozen, "enemy");
Check(f.Enemy.Health.Health == 19 && rowOther.Health == 19 && outcome.Recipients.Length == 2, "Movement must not recalculate an area's recipients.");
var filter = new DamageFilter(DamageType.Fire, "source");
Check(filter.Matches(DamageType.Fire, "source") && !filter.Matches(DamageType.Fire, "other")
    && !filter.Matches(DamageType.Ice, "source"), "Source-and-type filters default to both criteria.");
f = New(); f.Enemy.Health.Health = 10;
f.Resolve("enemy", new RestoreEffect(2), new DamageEffect(DamageType.Basic, 1));
var finalHealth = f.Enemy.Health.Health;
var replay = New(); replay.Enemy.Health.Health = 10;
replay.Resolve("enemy", new RestoreEffect(2), new DamageEffect(DamageType.Basic, 1));
Check(finalHealth == 11 && replay.Enemy.Health.Health == finalHealth, "Authored order and same-state deterministic resolution.");
Check(!typeof(CharacterData).Assembly.GetTypes().Where(type => type.Namespace == typeof(CharacterData).Namespace)
    .SelectMany(type => type.GetProperties()).Any(property => property.PropertyType.Namespace is { } ns && ns.StartsWith("NexusRealms.Prelude.Combat", StringComparison.Ordinal)),
    "DataModel must not expose combat runtime classes.");
var healingTarget = new DamageReceiver(18);
Check(HealingRules.Restore(healingTarget, 5) == 5 && healingTarget.Health == 23,
    "Healing must increase Health past its starting value without a gameplay maximum.");
Check(HealingRules.Restore(healingTarget, 5) == 5 && healingTarget.Health == 28,
    "Repeated healing must remain uncapped.");
healingTarget.Health = 0;
Check(HealingRules.Restore(healingTarget, 10) == 0 && healingTarget.Health == 0,
    "Healing must not resurrect defeated characters.");
healingTarget.Health = int.MaxValue - 1;
Check(HealingRules.Restore(healingTarget, double.MaxValue) == 1 && healingTarget.Health == int.MaxValue,
    "Healing must saturate without integer overflow.");
Reject(() => HealingRules.Restore(healingTarget, double.NaN), "Nonfinite healing must be rejected.");
var abilityFixture = New();
var paidAbility = ability with
{
    FocusCost = 1, TurnCost = 2, Targeting = new(TargetShape.Single),
};
abilityFixture.Player.Loadout.Learn(paidAbility, abilityRegistry);
var paidAction = abilityFixture.Player.Loadout.Actions[paidAbility.ActionId.Value];
abilityFixture.Resolver.Register(paidAction);
Check(!abilityFixture.Resolver.CanCommit("player", paidAction, "player"), "Ability targeting must reject the wrong team.");
var abilityPreview = abilityFixture.Resolver.Preview("player", paidAction, "enemy");
Check(abilityFixture.Player.Loadout.Focus == 2 && abilityFixture.Enemy.Health.Health == 20,
    "Ability preview must preserve live resources.");
abilityFixture.Resolver.Commit("player", paidAction, "enemy");
Check(abilityFixture.Player.Loadout.Focus == 3 && abilityFixture.Enemy.Health.Health == 19
    && abilityPreview.Characters["enemy"].Health == abilityFixture.Enemy.Health.Health,
    "Registry-bound execution must match preview and charge the ability Focus cost exactly once.");
var scaffoldFixture = New();
var scaffold = NexusRealms.Prelude.Database.Progression.PlaceholderAbilities.Slash;
scaffoldFixture.Player.Loadout.Learn(scaffold);
var scaffoldAction = scaffoldFixture.Player.Loadout.Actions[scaffold.ActionId.Value];
Check(!scaffoldFixture.Resolver.CanCommit("player", scaffoldAction, "enemy"),
    "Unfinished ability scaffolds must remain unavailable.");
Reject(() => scaffoldFixture.Player.Loadout.Learn(scaffold with { Id = "unknown", ActionId = "missing" }),
    "Unknown action mapping must fail before learning.");
Check(!scaffoldFixture.Player.Loadout.LearnedAbilities.Contains(new("unknown")), "Failed mapping must preserve progression.");
var dowseFixture = New();
var dowseAbility = NexusRealms.Prelude.Database.Progression.Aquarius.Dowse;
dowseFixture.Player.Loadout.Learn(dowseAbility);
var dowse = dowseFixture.Player.Loadout.Actions[dowseAbility.ActionId.Value];
dowseFixture.Resolver.RegisterAbility(dowseAbility);
Check(dowse is DowseAction && dowse.Icon == dowseAbility.Icon
    && dowseFixture.Player.Loadout.Find(dowse.Id) is { RequiresTarget: false },
    "Dowse must resolve its implemented action and need no target selection.");
Check(dowseFixture.Resolver.CanCommit("player", dowse, null), "Learned Dowse must be available without a target.");
var dowsePreview = dowseFixture.Resolver.Preview("player", dowse, null);
Check(dowsePreview.Characters["player"].Focus == 3 && dowseFixture.Player.Loadout.Focus == 2,
    "Dowse preview must grant exactly one Focus without changing live state.");
var dowseOutcome = dowseFixture.Resolver.Commit("player", dowse, null);
Check(dowseFixture.Player.Loadout.Focus == 4 && dowseFixture.Enemy.Loadout.Focus == 1
    && dowseFixture.Player.Health.Health == 20 && dowseFixture.Enemy.Health.Health == 20
    && dowseOutcome.Effects.Single().ActualChange == 1 && dowseOutcome.Recipients.SequenceEqual(["player"]),
    "Dowse must immediately grant one Focus to its caster without damaging anyone.");
var zeroFocusFixture = New();
zeroFocusFixture.Player.Loadout.Focus = 0;
zeroFocusFixture.Player.Loadout.Learn(dowseAbility);
var zeroFocusDowse = zeroFocusFixture.Player.Loadout.Actions[dowseAbility.ActionId.Value];
Check(zeroFocusFixture.Resolver.CanCommit("player", zeroFocusDowse, null), "Dowse must work at zero Focus.");
zeroFocusFixture.Resolver.Commit("player", zeroFocusDowse, null);
Check(zeroFocusFixture.Player.Loadout.Focus == 2, "Dowse must grant one Focus plus normal boundary regeneration without a Focus cost.");
ActionData LearnSupport(Fixture fixture, AbilityData definition)
{
    fixture.Player.Loadout.Learn(definition);
    var action = fixture.Player.Loadout.Actions[definition.ActionId.Value];
    fixture.Resolver.RegisterAbility(definition);
    return action;
}
// Aries control abilities must use the same paid execution in previews and gameplay.
foreach (var ariesDefinition in NexusRealms.Prelude.Database.Progression.Aries.Definitions)
    Check(AbilityActionRegistry.Default.Resolve(ariesDefinition).IsImplemented,
        $"Aries ability {ariesDefinition.Id} must not resolve to a scaffold.");

foreach (var focus in new[] { 0, 1, 2, 3, 5 })
{
    var shockFixture = New();
    shockFixture.Enemy.Loadout.Focus = focus;
    var shock = LearnSupport(shockFixture, NexusRealms.Prelude.Database.Progression.Aries.Shock);
    Check(shock is ShockAction && shockFixture.Resolver.CanCommit("player", shock, "enemy")
        && !shockFixture.Resolver.CanCommit("player", shock, "player")
        && !shockFixture.Resolver.CanCommit("player", shock, null),
        "Shock must require a living enemy target.");
    var shockPreview = shockFixture.Resolver.Preview("player", shock, "enemy");
    Check(shockPreview.Characters["enemy"].Focus == Math.Max(0, focus - 3)
        && shockFixture.Enemy.Loadout.Focus == focus && shockFixture.Player.Loadout.Focus == 2,
        "Shock preview must clamp Focus loss and leave live resources untouched.");
    var shockResult = shockFixture.Resolver.Commit("player", shock, "enemy");
    Check(shockFixture.Enemy.Loadout.Focus == shockPreview.Characters["enemy"].Focus
        && shockResult.Effects.Single().ActualChange == Math.Min(3, focus)
        && shockResult.Action.TurnCost == 0.8f
        && shockFixture.Player.Loadout.Focus == shockPreview.Characters["player"].Focus,
        "Shock commitment must match preview, including its activation cost.");
}

var intimidateFixture = New();
var intimidate = LearnSupport(intimidateFixture, NexusRealms.Prelude.Database.Progression.Aries.Intimidate);
var intimidatePreview = intimidateFixture.Resolver.Preview("player", intimidate, "enemy");
Check(intimidate is IntimidateAction
    && intimidatePreview.Schedule.Single(turn => turn.CombatantId == "enemy").Turn == 11.5f
    && intimidateFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 10,
    "Intimidate preview must delay only the isolated target by 1.5 turns.");
var intimidateResult = intimidateFixture.Resolver.Commit("player", intimidate, "enemy");
Check(intimidateFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 11.5f
    && intimidateFixture.Player.Loadout.Focus == 0 && intimidateResult.Action.TurnCost == 0.5f,
    "Intimidate must pay two Focus and delay the target's next action.");

foreach (var rowAbility in new[] { false, true })
{
    var berserkFixture = New();
    berserkFixture.Player.Loadout.Focus = 5;
    foreach (var (id, team, slot) in new[]
    {
        ("row-enemy", CombatTeam.Enemies, FormationSlot.FrontRight),
        ("other-row", CombatTeam.Enemies, FormationSlot.BackLeft),
        ("row-friend", CombatTeam.PlayerAndAllies, FormationSlot.FrontCenter),
    })
    {
        berserkFixture.Combat.Add(new(id, false, 10, team: team));
        berserkFixture.Resolver.Add(new(id, false, team, new(20), new(), slot));
    }
    var berserkAbility = rowAbility ? NexusRealms.Prelude.Database.Progression.Aries.Enrage
        : NexusRealms.Prelude.Database.Progression.Aries.Taunt;
    var berserkAction = LearnSupport(berserkFixture, berserkAbility);
    Check(berserkFixture.Resolver.CanCommit("player", berserkAction, "enemy")
        && !berserkFixture.Resolver.CanCommit("player", berserkAction, null),
        "Berserk abilities must require an enemy anchor.");
    var berserkPreview = berserkFixture.Resolver.Preview("player", berserkAction, "enemy");
    var expectedTargets = rowAbility ? new[] { "enemy", "row-enemy" } : new[] { "enemy" };
    Check(berserkPreview.Outcome.Recipients.SequenceEqual(expectedTargets)
        && berserkFixture.Resolver.Characters.Values.All(character => character.Statuses.Count == 0),
        "Berserk previews must isolate single-target or enemy-row selection.");
    berserkFixture.Resolver.Commit("player", berserkAction, "enemy");
    foreach (var character in berserkFixture.Resolver.Characters.Values)
        Check(expectedTargets.Contains(character.Id)
            ? character.Statuses.Single().Definition.Id == "berserk" && character.Statuses.Single().ExpiresAt == 2
            : character.Statuses.Count == 0,
            "Taunt and Enrage must apply exactly two timeline turns of Berserk only to their recipients.");
    Check(berserkPreview.Characters["player"].Focus == 5 - berserkAbility.FocusCost
        && berserkFixture.Player.Loadout.Focus == berserkPreview.Characters["player"].Focus + 1,
        "Berserk abilities must pay their Focus cost; live recovery adds one boundary regeneration.");
    berserkFixture.Combat.SubmitAction(new(1, (_, _) => { }));
    Check(berserkFixture.Resolver.Characters.Values.All(character => character.Statuses.Count == 0),
        "Taunt and Enrage must expire at the second timeline boundary.");
}
foreach (var controlDefinition in new[]
{
    NexusRealms.Prelude.Database.Progression.Aries.Enrage,
    NexusRealms.Prelude.Database.Progression.Aries.Intimidate,
    NexusRealms.Prelude.Database.Progression.Aries.Shock,
    NexusRealms.Prelude.Database.Progression.Aries.Taunt,
})
{
    var controlFixture = New();
    var controlAction = LearnSupport(controlFixture, controlDefinition);
    controlFixture.Player.Loadout.Focus = 0;
    Check(!controlFixture.Resolver.CanCommit("player", controlAction, "enemy"),
        "Aries control abilities must reject insufficient Focus.");
    controlFixture.Player.Loadout.Focus = 5;
    controlFixture.Enemy.Loadout.Focus = 5;
    Apply(controlFixture, "evade");
    var controlPreview = controlFixture.Resolver.Preview("player", controlAction, "enemy");
    var controlResult = controlFixture.Resolver.Commit("player", controlAction, "enemy");
    Check(controlResult.Effects.All(effect => effect.Result == EffectResult.Evaded)
        && controlPreview.Outcome.Effects.All(effect => effect.Result == EffectResult.Evaded)
        && controlPreview.Characters["enemy"].Focus == 5
        && controlFixture.Enemy.Loadout.Focus == (controlDefinition.TurnCost >= 1 ? 6 : 5)
        && controlFixture.Enemy.Statuses.Count == 1
        && controlFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 10
        && controlPreview.Characters["player"].Focus == 5 - controlDefinition.FocusCost,
        "Evasion must cancel Aries control effects while preserving their activation costs.");
}
Console.WriteLine("Remaining Aries ability checks passed.");

var support = New();
var heal = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aquarius.Heal);
Check(support.Player.Loadout.Find(heal.Id) is { RequiresTarget: false }
    && support.Resolver.CanCommit("player", heal, null), "Heal must restore the caster without target selection.");
var healPreview = support.Resolver.Preview("player", heal, null);
Check(healPreview.Characters["player"].Health == 23 && support.Player.Health.Health == 20,
    "Heal preview must exceed starting Health without changing live state.");
support.Resolver.Commit("player", heal, null);
Check(support.Player.Health.Health == 23 && support.Enemy.Health.Health == 20,
    "Heal must add three Health with no gameplay maximum.");
support.Resolver.Commit("player", heal, null);
Check(support.Player.Health.Health == 26, "Repeated Heal must keep increasing Health.");

support = New();
support.Player.Health.Health = 10;
support.Player.Loadout.Focus = 4;
var recovery = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Recovery);
Check(recovery is RecoveryAction && recovery.Costs.Single() is
    { Subject: ResourceSubject { Resource: ResourceKind.Focus }, Amount: 1 }
    && support.Resolver.CanCommit("player", recovery, null),
    "Recovery must use its authored one-Focus cost and be available as a self action.");
var recoveryPreview = support.Resolver.Preview("player", recovery, null);
Check(recoveryPreview.Characters["player"].Health == 18
    && recoveryPreview.Outcome.Effects.Single(effect => effect.Verb == EffectVerb.Consume).ActualChange == 3
    && recoveryPreview.Outcome.Effects.Single(effect => effect.Verb == EffectVerb.Restore).ActualChange == 8
    && support.Player.Health.Health == 10 && support.Player.Loadout.Focus == 4,
    "Recovery preview must count the activation cost, consume remaining Focus, and restore two Health per initial Focus without mutating live state.");
var recoveryOutcome = support.Resolver.Commit("player", recovery, null);
Check(support.Player.Health.Health == 18
    && recoveryOutcome.Effects.Single(effect => effect.Verb == EffectVerb.Consume).ActualChange == 3
    && recoveryOutcome.Effects.Single(effect => effect.Verb == EffectVerb.Restore).ActualChange == 8,
    "Recovery must spend all four Focus and restore eight Health.");
support = New();
support.Player.Health.Health = 5;
support.Player.Loadout.Focus = 1;
recovery = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Recovery);
var minimumRecoveryOutcome = support.Resolver.Commit("player", recovery, null);
Check(support.Player.Health.Health == 7
    && minimumRecoveryOutcome.Effects.Single(effect => effect.Verb == EffectVerb.Restore).ActualChange == 2,
    "Recovery must count its one-Focus activation cost when no Focus remains for the consume effect.");

var kingMakerScale = new[] { 1, 2, 3, 5, 8, 13, 19, 26, 34, 60, 94 };
for (var focus = 0; focus < kingMakerScale.Length; focus++)
{
    support = New();
    support.Player.Loadout.Focus = focus;
    var kingMaker = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.KingMaker);
    Check(kingMaker is KingMakerAction && (kingMaker.Flags & ActionFlags.Melee) != 0
        && support.Resolver.CanCommit("player", kingMaker, "enemy"),
        "King Maker must be a usable melee action at every Focus amount.");
    var kingMakerOutcome = support.Resolver.ResolveCommitted(kingMaker, "player", ["enemy"]);
    var focusSpent = kingMakerOutcome.Effects.Single(effect => effect.Requested is
        ConsumeEffect { Subject: ResourceSubject { Resource: ResourceKind.Focus } });
    var bonusDamage = kingMakerOutcome.Effects.Single(effect => effect.EffectId == "king-maker-bonus");
    Check(focusSpent.ActualChange == focus && support.Player.Loadout.Focus == 0
        && bonusDamage.RequestedAmount == kingMakerScale[focus]
        && bonusDamage.ResolvedAmount == kingMakerScale[focus],
        "King Maker must consume the indexed Focus amount and deal its authored or continued bonus.");
}

support = New();
support.Player.Loadout.Focus = 5;
var previewKingMaker = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.KingMaker);
var kingMakerPreview = support.Resolver.Preview("player", previewKingMaker, "enemy");
Check(kingMakerPreview.Outcome.Effects.Single(effect => effect.EffectId == "king-maker-bonus").ResolvedAmount == 13
    && kingMakerPreview.Outcome.Effects.Single(effect => effect.Requested is ConsumeEffect).ActualChange == 5
    && support.Player.Loadout.Focus == 5 && support.Enemy.Health.Health == 20,
    "King Maker preview must scale from and consume only its isolated Focus snapshot.");

support = New();
Apply(support, "positive", "enemy"); Apply(support, "negative-permanent", "enemy");
Apply(support, "positive", "player");
var purge = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aquarius.Purge);
var purgePreview = support.Resolver.Preview("player", purge, "enemy");
Check(purgePreview.Characters["enemy"].Statuses.IsEmpty && support.Enemy.Statuses.Count == 2,
    "Purge preview must remove both status kinds without mutating live state.");
support.Resolver.Commit("player", purge, "enemy");
Check(support.Enemy.Statuses.Count == 0 && support.Player.Statuses.Count == 1,
    "Purge must remove all statuses only from its target.");

support = New();
Apply(support, "negative-long", "player", new(StatusClock.RecipientTurnStart, 4));
Apply(support, "negative-short", "player", new(StatusClock.RecipientTurnStart, 1));
Apply(support, "negative-clock", "player", new(StatusClock.Timeline, 4));
Apply(support, "negative-permanent", "player");
Apply(support, "positive", "player", new(StatusClock.Timeline, 4));
var cleanse = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aquarius.Cleanse);
var beforeLong = support.Player.Statuses.Single(status => status.Definition.Id.Value == "negative-long").ExpiresOnTurnStart;
var cleansePreview = support.Resolver.Preview("player", cleanse, null);
Check(support.Player.Statuses.Count == 5
    && support.Player.Statuses.Single(status => status.Definition.Id.Value == "negative-clock").ExpiresAt == 4,
    "Cleanse preview must not mutate live expiration boundaries.");
var cleanseResult = support.Resolver.Commit("player", cleanse, null);
Check(!support.Player.Statuses.Any(status => status.Definition.Id.Value == "negative-short")
    && support.Player.Statuses.Single(status => status.Definition.Id.Value == "negative-long").ExpiresOnTurnStart == beforeLong - 1
    && support.Player.Statuses.Single(status => status.Definition.Id.Value == "negative-clock").ExpiresAt == 3
    && support.Player.Statuses.Single(status => status.Definition.Id.Value == "positive").ExpiresAt == 4
    && support.Player.Statuses.Any(status => status.Definition.Id.Value == "negative-permanent")
    && cleanseResult.Effects.Single().ActualChange == 3,
    "Cleanse must shorten negative timed statuses, remove exhausted ones, and preserve positive and permanent effects.");
Check(cleansePreview.Characters["player"].Statuses.SequenceEqual(
    support.Player.Statuses.OrderBy(status => status.Sequence).Select(status => FormattableString.Invariant($"{status.Definition.Id}:{status.Sequence}:{status.RemainingUses}:{status.ExpiresAt}:{status.ExpiresOnTurnStart}"))),
    "Cleanse preview and live expiration state must agree.");

support = New();
Apply(support, "negative-long", "player");
Apply(support, "negative-short", "player");
Apply(support, "negative-clock", "player");
Apply(support, "positive", "player");
var defiance = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Defiance);
var defiancePreview = support.Resolver.Preview("player", defiance, null);
Check(support.Player.Statuses.Count == 4
    && defiancePreview.Characters["player"].Statuses.Length == 2
    && defiancePreview.Characters["player"].Statuses
        .Any(status => status.StartsWith("berserk:", StringComparison.Ordinal) && status.Split(':')[3] == "3"),
    "Defiance preview must count every negative status without mutating live statuses.");
var defianceOutcome = support.Resolver.Commit("player", defiance, null);
Check(support.Player.Statuses.Count == 2
    && support.Player.Statuses.Any(status => status.Definition.Id.Value == "positive")
    && support.Player.Statuses.Single(status => status.Definition.Id.Value == "berserk").ExpiresAt == 3
    && defianceOutcome.Effects[0].ActualChange == 3 && defianceOutcome.Effects[1].ActualChange == 1,
    "Defiance must remove all negative statuses and replace them with Berserk for one turn per removed status.");
support = New();
Apply(support, "negative-long", "player");
Apply(support, "berserk", "player", new(StatusClock.Timeline, 5));
var defiancePlan = new DefianceAction().Execute(support.Resolver, "player");
Check(defiancePlan.OfType<ApplyEffect>().Single().Persistence.Duration == 2,
    "Defiance must include a current Berserk status when planning its replacement duration.");
support = New();
defiance = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Defiance);
Check(support.Resolver.CanCommit("player", defiance, null)
    && support.Resolver.Preview("player", defiance, null).Characters["player"].Statuses.IsEmpty,
    "Defiance with no negative statuses must remain valid and not apply zero-duration Berserk.");

support = New();
var fury = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Fury);
Check(fury is FuryAction && (fury.Flags & ActionFlags.Melee) != 0
    && support.Resolver.CanCommit("player", fury, "enemy"),
    "Fury must be a usable single-target melee attack.");
var furyOutcome = support.Resolver.Commit("player", fury, "enemy");
Check(furyOutcome.Effects[^1] is { Verb: EffectVerb.Apply, RecipientId: "player", ActualChange: 1 }
    && furyOutcome.Effects.Any(effect => effect.Verb == EffectVerb.Damage && effect.ActualChange > 0)
    && support.Enemy.Health.Health < 20
    && MathF.Abs(support.Resolver.EffectiveTurnCost("player", 1) - 0.75f) < 0.0001f,
    "Fury must strike its target, apply its speed boost to the caster, and retain it for the caster's next turn.");
support = New();
fury = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Fury);
support.Resolver.ResolveCommitted(fury, "player", ["enemy"]);
support.Resolver.ResolveCommitted(fury, "player", ["enemy"]);
Check(support.Player.Statuses.Count(status => status.Definition.Id == "fury") == 2
    && MathF.Abs(support.Resolver.EffectiveTurnCost("player", 1) - 0.5625f) < 0.0001f,
    "Fury speed boosts must stack multiplicatively.");
var dualWieldLoadout = CombatLoadout.FromState(
    [
        WeaponCatalog.Create(WeaponType.Sword, "fury-right", "Sword", "Right hand"),
        WeaponCatalog.Create(WeaponType.Axe, "fury-left", "Axe", "Left hand"),
    ],
    new Dictionary<string, string>
    {
        ["Right hand"] = "fury-right",
        ["Left hand"] = "fury-left",
    },
    focus: 2);
support = new Fixture(definitions, dualWieldLoadout);
fury = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Fury);
var dualFuryOutcome = support.Resolver.ResolveCommitted(fury, "player", ["enemy"]);
Check(dualFuryOutcome.Effects
        .Where(effect => effect.Requested is DamageEffect)
        .Select(effect => ((DamageEffect)effect.Requested).AttackIndex)
        .Distinct()
        .Count() == 2,
    "Fury must attack once with each of two equipped melee weapons.");
Apply(support, "onslaught", "player", NexusRealms.Prelude.Database.StatusEffects.Onslaught.Persistence);
dualFuryOutcome = support.Resolver.ResolveCommitted(fury, "player", ["enemy"]);
var onslaughtDualBonuses = dualFuryOutcome.Effects
    .Where(effect => effect.Requested is DamageEffect { Type: DamageType.Basic })
    .ToArray();
Check(onslaughtDualBonuses.Length == 2
    && onslaughtDualBonuses.All(effect => effect.ResolvedAmount == 1)
    && onslaughtDualBonuses.Select(effect => ((DamageEffect)effect.Requested).AttackIndex).Distinct().Count() == 2,
    "Onslaught must add one Basic damage to each dual-wielded melee attack.");

support = New();
var onslaught = LearnSupport(support, NexusRealms.Prelude.Database.Progression.Aries.Onslaught);
Check(onslaught is OnslaughtAction
    && onslaught.Targeting.Shape == TargetShape.Self
    && support.Resolver.CanCommit("player", onslaught, null),
    "Onslaught must be a usable self-targeted ability.");
support.Resolver.ResolveCommitted(onslaught, "player", ["player"]);
Check(support.Player.Statuses.Single().Definition.Id == "onslaught",
    "Onslaught must apply its status to the caster.");
var meleeWithoutBasic = new ExampleAction("melee-without-basic", new DamageEffect(DamageType.Slashing, 3))
    { Flags = ActionFlags.Melee };
var meleeOutcome = support.Resolver.ResolveCommitted(meleeWithoutBasic, "player", ["enemy"]);
Check(support.Enemy.Health.Health == 16
    && meleeOutcome.Effects.Any(effect => effect.Requested is DamageEffect { Type: DamageType.Basic, Amount: 0 }
        && effect.ResolvedAmount == 1),
    "Onslaught must create one Basic damage point on a melee attack without a Basic entry.");
var nonMelee = new ExampleAction("non-melee", new DamageEffect(DamageType.Slashing, 3));
var nonMeleeOutcome = support.Resolver.ResolveCommitted(nonMelee, "player", ["enemy"]);
Check(support.Enemy.Health.Health == 13 && nonMeleeOutcome.Effects.Length == 1
    && nonMeleeOutcome.Effects.Single().Requested is DamageEffect { Type: DamageType.Slashing, Amount: 3 },
    "Onslaught must not modify damage from actions without the Melee flag.");

// The shortened expiration must be scheduled, including when no actor acts at that time.
support = New();
Apply(support, "negative-clock", "enemy", new(StatusClock.Timeline, 2));
support.Resolve("enemy", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
support.Combat.SubmitAction(new(1.5f, (_, _) => { }));
Check(!support.Enemy.Statuses.Any(), "A shortened timeline status must expire at its new boundary.");
support = New();
Apply(support, "negative-clock", "player", new(StatusClock.Timeline, 0.5f));
support.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(support.Player.Statuses.Count == 0, "Short remaining timeline duration must expire immediately.");
support = New();
Apply(support, "negative-permanent", "player", new(Uses: 2, UseEvent: StatusUseEvent.IncomingAction));
support.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(support.Player.Statuses.Single().RemainingUses == 1,
    "Cleanse must not shorten use counts; only ordinary incoming-action consumption applies.");
Reject(() => CombatRules.Validate(new EffectAction("bad-cleanse", new DispelEffect(new(), DurationReduction: -1)), definitions),
    "Negative duration reductions must be rejected.");
void AddWindCharacter(Fixture fixture, string id, FormationSlot position, CombatTeam team = CombatTeam.Enemies)
{
    fixture.Combat.Add(new(id, false, 10, team: team));
    fixture.Resolver.Add(new(id, false, team, new(20), new(), position));
}
var wind = New();
AddWindCharacter(wind, "z-middle", FormationSlot.MiddleLeft);
AddWindCharacter(wind, "z-back", FormationSlot.BackRight);
AddWindCharacter(wind, "friend", FormationSlot.MiddleRight, CombatTeam.PlayerAndAllies);
var gale = LearnSupport(wind, NexusRealms.Prelude.Database.Progression.Aquarius.Gale);
Check(wind.Player.Loadout.Find(gale.Id) is { RequiresTarget: false }
    && wind.Resolver.CanCommit("player", gale, null), "Gale must target all enemies without an anchor.");
var galePreview = wind.Resolver.Preview("player", gale, null);
Check(galePreview.Characters["enemy"].Position == FormationSlot.MiddleLeft
    && galePreview.Characters["z-middle"].Position == FormationSlot.BackLeft
    && galePreview.Characters["z-back"].Position == FormationSlot.BackRight
    && wind.Enemy.Position == FormationSlot.FrontLeft,
    "Gale preview must vacate leading rows first and leave live formation unchanged.");
var movements = new List<string>();
wind.Resolver.Moved += (id, _) => movements.Add(id);
var galeOutcome = wind.Resolver.Commit("player", gale, null);
Check(wind.Enemy.Position == FormationSlot.MiddleLeft
    && wind.Resolver.Characters["z-middle"].Position == FormationSlot.BackLeft
    && wind.Resolver.Characters["z-back"].Position == FormationSlot.BackRight
    && wind.Resolver.Characters["friend"].Position == FormationSlot.MiddleRight
    && movements.Count == 2 && !galeOutcome.Recipients.Contains("friend")
    && wind.Enemy.Health.Health == 20 && wind.Player.Health.Health == 20,
    "Gale must shift each eligible enemy once, preserve allies and boundaries, and emit movement presentation events.");
Check(wind.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Row == CombatRow.Middle
    && wind.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "z-middle").Row == CombatRow.Back,
    "Gale must update authoritative scheduler rows.");
foreach (var id in galeOutcome.Recipients)
    Check(galePreview.Characters[id].Position == wind.Resolver.Characters[id].Position, "Gale preview and commit must agree.");

wind = New();
AddWindCharacter(wind, "z-middle", FormationSlot.MiddleLeft);
AddWindCharacter(wind, "z-back", FormationSlot.BackLeft);
var tailwind = LearnSupport(wind, NexusRealms.Prelude.Database.Progression.Aquarius.Tailwind);
var tailwindPreview = wind.Resolver.Preview("player", tailwind, null);
var tailwindOutcome = wind.Resolver.Commit("player", tailwind, null);
Check(wind.Enemy.Position == FormationSlot.FrontLeft
    && wind.Resolver.Characters["z-middle"].Position == FormationSlot.FrontCenter
    && wind.Resolver.Characters["z-back"].Position == FormationSlot.MiddleLeft,
    "Tailwind must move forward, use the nearest free slot, and leave front-edge enemies in place.");
foreach (var id in tailwindOutcome.Recipients)
    Check(tailwindPreview.Characters[id].Position == wind.Resolver.Characters[id].Position, "Tailwind preview and commit must agree.");
Check(wind.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "z-middle").Row == CombatRow.Front,
    "Tailwind must update the scheduler's formation row.");

wind = New();
foreach (var slot in Enum.GetValues<FormationSlot>().Where(slot => slot != FormationSlot.FrontLeft))
    AddWindCharacter(wind, slot.ToString(), slot);
gale = LearnSupport(wind, NexusRealms.Prelude.Database.Progression.Aquarius.Gale);
tailwind = LearnSupport(wind, NexusRealms.Prelude.Database.Progression.Aquarius.Tailwind);
var fullFormation = wind.Resolver.Characters.ToDictionary(pair => pair.Key, pair => pair.Value.Position);
Check(wind.Resolver.Commit("player", gale, null).Effects.All(effect => effect.ActualChange == 0)
    && wind.Resolver.Commit("player", tailwind, null).Effects.All(effect => effect.ActualChange == 0)
    && fullFormation.All(pair => wind.Resolver.Characters[pair.Key].Position == pair.Value),
    "Full adjacent rows must prevent movement without swaps, skipped rows or duplicate occupancy.");

wind = New();
AddWindCharacter(wind, "z-middle", FormationSlot.MiddleLeft);
Apply(wind, "evade", "z-middle");
gale = LearnSupport(wind, NexusRealms.Prelude.Database.Progression.Aquarius.Gale);
galeOutcome = wind.Resolver.Commit("player", gale, null);
Check(wind.Resolver.Characters["z-middle"].Position == FormationSlot.MiddleLeft
    && wind.Enemy.Position == FormationSlot.MiddleCenterLeft
    && galeOutcome.Effects.Single(effect => effect.RecipientId == "z-middle").Result == EffectResult.Evaded,
    "Evaded movement must keep its occupied slot, and other recipients must respect it.");
wind = New();
gale = LearnSupport(wind, NexusRealms.Prelude.Database.Progression.Aquarius.Gale);
wind.Enemy.Health.Health = 0;
Check(!wind.Resolver.CanCommit("player", gale, null), "Wind actions need at least one living enemy.");
var movePlayer = new EffectAction("shift-player", new ShiftRowEffect(RowShiftDirection.Forward)) { Targeting = new(TargetShape.Self) };
wind = New(); wind.Player.Loadout.Actions.Add(movePlayer.Id, movePlayer);
Check(!wind.Resolver.CanCommit("player", movePlayer, null), "Relative row shifts must reject the player without a formation position.");
Reject(() => CombatRules.Validate(new EffectAction("bad-shift", new ShiftRowEffect((RowShiftDirection)99)), definitions),
    "Unknown row shift directions must be rejected.");
var breezeFixture = New();
var breeze = LearnSupport(breezeFixture, NexusRealms.Prelude.Database.Progression.Aquarius.CoolingBreeze);
Check(breeze is CoolingBreezeAction && breeze.Targeting.Shape == TargetShape.Self
    && breezeFixture.Player.Loadout.Find(breeze.Id) is { RequiresTarget: false }
    && breezeFixture.Resolver.CanCommit("player", breeze, null), "Cooling Breeze must protect the caster without target selection.");
breezeFixture.Resolver.ScheduleEvent(new(0.5f, "incoming fire", "enemy", "player", new EffectAction("fire-hazard", new DamageEffect(DamageType.Fire, 5))));
var breezePreview = breezeFixture.Resolver.Preview("player", breeze, null);
Check(breezePreview.Outcome.Effects.Single().Verb == EffectVerb.Apply
    && breezePreview.Outcome.Recipients.SequenceEqual(["player"])
    && breezePreview.Characters["player"].Health == 20
    && breezeFixture.Player.Statuses.Count == 0,
    "Cooling Breeze preview must protect only the caster without changing live statuses.");
var breezeOutcome = breezeFixture.Resolver.Commit("player", breeze, null);
Check(breezeOutcome.Effects.Single().ActualChange == 1 && breezeOutcome.Recipients.SequenceEqual(["player"])
    && breezeFixture.Player.Statuses.Single().ExpiresAt == 3 && breezeFixture.Player.Health.Health == 20,
    "Cooling Breeze must protect the caster during recovery and remain active until timeline turn three.");

// Resolve application at an enemy decision boundary so attacks can land during the interval.
breezeFixture = New();
breezeFixture.Resolver.ResolveCommitted(breeze, "player", ["enemy"]);
var protectedState = breezeFixture.Resolver.CopyForSimulation();
var mixedDamage = new EffectAction("mixed-fire", new DamageEffect(DamageType.Fire, 5), new DamageEffect(DamageType.Ice, 2));
var protectedOutcome = breezeFixture.Resolver.ResolveCommitted(mixedDamage, "player", ["enemy"]);
Check(breezeFixture.Enemy.Health.Health == 18 && protectedOutcome.Effects[0].Result == EffectResult.Negated
    && protectedOutcome.Effects[1].ActualChange == 2,
    "Cooling Breeze must negate Fire while allowing other damage types.");
breezeFixture.Resolve("enemy", new DamageEffect(DamageType.Fire, 3));
Check(breezeFixture.Enemy.Health.Health == 18 && breezeFixture.Enemy.Statuses.Count == 1,
    "Protection must negate repeated Fire hits without being consumed.");
protectedState.ResolveCommitted(mixedDamage, "player", ["enemy"]);
Check(protectedState.Characters["enemy"].Health.Health == 18,
    "Copied simulations must preserve Cooling Breeze rules.");
breezeFixture.Combat.SubmitAction(new(3, (_, _) => { }));
Check(breezeFixture.Enemy.Statuses.Count == 0, "Cooling Breeze must expire exactly at its timeline boundary.");
breezeFixture.Resolve("enemy", new DamageEffect(DamageType.Fire, 3));
Check(breezeFixture.Enemy.Health.Health == 15, "Fire damage must resume after Cooling Breeze expires.");

breezeFixture = New();
breezeFixture.Resolve("enemy", new ApplyEffect("cooling-breeze", new(StatusClock.Timeline, 1)));
breezeFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
breezeFixture.Resolve("enemy", new ApplyEffect("cooling-breeze", new(StatusClock.Timeline, 1)));
Check(breezeFixture.Enemy.Statuses.Count == 1 && breezeFixture.Enemy.Statuses.Single().ExpiresAt == 1.5f,
    "Recasting Cooling Breeze must refresh without stacking.");
breezeFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
Check(breezeFixture.Enemy.Statuses.Count == 1, "Original expiration must not remove refreshed protection.");
breezeFixture.Resolve("enemy", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(breezeFixture.Enemy.Statuses.Single().ExpiresAt == 1.5f, "Cleanse must leave positive protection unchanged.");
breezeFixture.Resolve("enemy", new DispelEffect(new()));
Check(breezeFixture.Enemy.Statuses.Count == 0, "Purge must remove Cooling Breeze.");
var levitation = New();
levitation.Player.Loadout.Focus = 4;
var levitate = LearnSupport(levitation, NexusRealms.Prelude.Database.Progression.Aquarius.Levitate);
var levitatePreview = levitation.Resolver.Preview("player", levitate, null);
Check(levitation.Player.Elevation == 0 && levitation.Player.Loadout.Focus == 4
    && levitatePreview.Characters["player"].Focus == 3,
    "Levitate preview must calculate duration after payment without mutating live Focus or elevation.");
levitation.Resolver.Commit("player", levitate, null);
Check(levitation.Player.Elevation == 1 && levitation.Player.Loadout.Focus == 4
    && levitation.Player.Statuses.Single().ExpiresAt == 3,
    "Four Focus must pay one and grant three timeline turns of elevation.");
var shortMelee = new EffectAction("short-melee", new DamageEffect(DamageType.Basic, 2)) { MeleeRange = WeaponRange.Short };
var mediumMelee = shortMelee with { Id = "medium-melee", MeleeRange = WeaponRange.Medium };
Check(!levitation.Resolver.CanTarget("enemy", shortMelee, "player")
    && levitation.Resolver.CanTarget("enemy", mediumMelee, "player"),
    "An elevated player must be unreachable by short melee but remain reachable by longer melee.");
levitation.Resolver.ResolveCommitted(shortMelee, "enemy", ["player"]);
Check(levitation.Player.Health.Health == 20, "Already committed short melee must not damage an elevated player.");
levitation.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(levitation.Player.Elevation == 1, "Cleanse must preserve positive elevation.");
levitation.Resolve("player", new DispelEffect(new()));
Check(levitation.Player.Elevation == 0 && levitation.Resolver.CanTarget("enemy", shortMelee, "player"),
    "Purge must immediately restore short-melee eligibility.");

levitation = New();
levitation.Player.Loadout.Focus = 4;
levitate = LearnSupport(levitation, NexusRealms.Prelude.Database.Progression.Aquarius.Levitate);
levitation.Resolver.Commit("player", levitate, null);
levitation.Combat.SubmitAction(new(1, (_, _) => { }));
Check(levitation.Player.Elevation == 1, "Elevation must persist before its Focus-derived expiry.");
levitation.Combat.SubmitAction(new(1, (_, _) => { }));
Check(levitation.Player.Elevation == 0, "Elevation must expire at its exact Focus-derived timeline boundary.");
levitation = New(); levitation.Player.Loadout.Focus = 0;
levitate = LearnSupport(levitation, NexusRealms.Prelude.Database.Progression.Aquarius.Levitate);
Check(!levitation.Resolver.CanCommit("player", levitate, null), "Levitate requires its one Focus cost.");
levitation.Player.Loadout.Focus = 1;
Check(!levitation.Resolver.CanCommit("player", levitate, null)
    && !levitation.Player.Loadout.Available(levitation.Player.Loadout.Find(levitate.Id)!),
    "Levitate must be disabled in commitment and the HUD with only one Focus.");
Reject(() => levitation.Resolver.Commit("player", levitate, null), "One Focus cannot commit Levitate.");
Check(levitation.Player.Loadout.Focus == 1, "Rejected Levitate must not spend Focus.");
levitation.Player.Loadout.Focus = 2;
Check(levitation.Resolver.CanCommit("player", levitate, null)
    && levitation.Player.Loadout.Available(levitation.Player.Loadout.Find(levitate.Id)!),
    "Levitate must become enabled at two Focus.");

levitation = New();
levitation.Resolve("enemy", new ApplyEffect("elevated", new(StatusClock.Timeline, 5)));
levitation.Player.Loadout.Actions.Add(shortMelee.Id, shortMelee);
levitation.Player.Loadout.Actions.Add(mediumMelee.Id, mediumMelee);
Check(!levitation.Resolver.CanCommit("player", shortMelee, "enemy")
    && levitation.Resolver.CanCommit("player", mediumMelee, "enemy"),
    "An elevated enemy in the front row must be excluded from short-melee targeting.");
Check(!CandidateGeneration.Enumerate(levitation.Resolver, "player", includeLayouts: false)
    .Any(candidate => candidate.TargetId == "enemy" && (candidate.Action.Id == shortMelee.Id
        || candidate.Action.Id is "LeftHandStrike" or "RightHandStrike")),
    "AI must exclude typed short melee, swords and unarmed strikes against elevated enemies.");
Check(levitation.Player.Loadout.ValidTarget(CombatLoadout.Throw,
    new(true, true, CombatTeam.Enemies, CombatRow.Front, Elevated: true)),
    "Thrown attacks must remain able to target elevated enemies.");
var playerSwordIndex = levitation.Player.Loadout.Items.FindIndex(item => item.Weapon is not null);
var playerSword = levitation.Player.Loadout.Items[playerSwordIndex];
levitation.Player.Loadout.Items[playerSwordIndex] = playerSword with { Weapon = playerSword.Weapon! with { Range = WeaponRange.Medium } };
Check(levitation.Player.Loadout.ValidTarget(CombatLoadout.RightHandStrike,
    new(true, true, CombatTeam.Enemies, CombatRow.Front, Elevated: true)),
    "Medium melee weapons must reach elevated targets.");
levitation.Player.Loadout.Items[playerSwordIndex] = playerSword with { Weapon = playerSword.Weapon! with { IsMelee = false } };
Check(levitation.Player.Loadout.ValidTarget(CombatLoadout.RightHandStrike,
    new(true, true, CombatTeam.Enemies, CombatRow.Front, Elevated: true)),
    "Short-range ranged weapons must not be mistaken for short melee.");

var groundAttack = new EffectAction("ground-wave", new DamageEffect(DamageType.Basic, 5), new DelayEffect(1))
    { RequiresGroundContact = true };
var groundOutcome = levitation.Resolver.ResolveCommitted(groundAttack, "player", ["enemy"]);
Check(groundOutcome.Effects.All(effect => effect.Result == EffectResult.Negated) && levitation.Enemy.Health.Health == 20,
    "An airborne recipient must negate all target operations of a ground-dependent attack.");
var partialGround = new EffectAction("mixed-ground", new DamageEffect(DamageType.Basic, 5) { RequiresGroundContact = true }, new RestoreEffect(2));
var mixedGroundOutcome = levitation.Resolver.ResolveCommitted(partialGround, "player", ["enemy"]);
Check(mixedGroundOutcome.Effects[0].Result == EffectResult.Negated && levitation.Enemy.Health.Health == 22,
    "Individual ground effects must be negated while unrelated effects still resolve.");
var levitationCopy = levitation.Resolver.CopyForSimulation();
Check(levitationCopy.Characters["enemy"].Elevation == 1
    && !levitationCopy.CanTarget("player", shortMelee, "enemy"), "Simulation copies must preserve elevation and reach.");
levitation.Resolve("enemy", new ApplyEffect("elevated", new(StatusClock.Timeline, 6)));
Check(levitation.Enemy.Elevation == 1 && levitation.Enemy.Statuses.Count == 1,
    "Repeated Levitate must refresh instead of stacking elevation.");
levitation.Resolve("enemy", new DispelEffect(new()));
levitation.Resolver.ResolveCommitted(groundAttack, "player", ["enemy"]);
Check(levitation.Enemy.Health.Health == 17, "Ground-dependent damage must resume immediately after landing.");
Reject(() => CombatRules.ValidateStatus(NexusRealms.Prelude.Database.StatusEffects.Elevated with { Elevation = -1 }),
    "Negative status elevation must be rejected.");
var shieldFixture = New(); shieldFixture.Player.Loadout.Focus = 4;
var pressureShield = LearnSupport(shieldFixture, NexusRealms.Prelude.Database.Progression.Aquarius.PressureShield);
var shieldPreview = shieldFixture.Resolver.Preview("player", pressureShield, null);
Check(shieldPreview.Characters["player"].Focus == 3 && shieldFixture.Player.Statuses.Count == 0,
    "Pressure Shield preview must calculate duration after its one-Focus cost without changing live state.");
shieldFixture.Resolver.Commit("player", pressureShield, null);
Check(shieldFixture.Player.Statuses.Single().ExpiresAt == 3 && shieldFixture.Player.Loadout.Focus == 4,
    "Pressure Shield must last one timeline turn per Focus left after payment.");
var physicalMix = new EffectAction("physical-mix", new DamageEffect(DamageType.Slashing, 1),
    new DamageEffect(DamageType.Piercing, 1), new DamageEffect(DamageType.Crushing, 1));
shieldFixture.Resolver.ResolveCommitted(physicalMix, "enemy", ["player"]);
Check(shieldFixture.Player.Health.Health == 18, "Pressure Shield must reduce total Physical damage by one, not one per type.");
shieldFixture.Resolver.ResolveCommitted(physicalMix, "enemy", ["player"]);
Check(shieldFixture.Player.Health.Health == 16, "Pressure Shield resistance must renew for each incoming attack.");
shieldFixture.Resolver.ResolveCommitted(new EffectAction("nonphysical", new DamageEffect(DamageType.Basic, 2), new DamageEffect(DamageType.Fire, 2)), "enemy", ["player"]);
Check(shieldFixture.Player.Health.Health == 12, "Pressure Shield must not reduce Basic or elemental damage.");
shieldFixture.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(shieldFixture.Player.Statuses.Single().ExpiresAt == 3, "Cleanse must leave positive Pressure Shield unchanged.");
shieldFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(shieldFixture.Player.Statuses.Count == 1, "Pressure Shield must last until its Focus-derived expiration.");
shieldFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(shieldFixture.Player.Statuses.Count == 0, "Pressure Shield must expire on schedule.");
shieldFixture.Resolve("player", new ApplyEffect("pressure-shield", new(StatusClock.Timeline, 5)));
shieldFixture.Resolve("player", new ApplyEffect("pressure-shield", new(StatusClock.Timeline, 5)));
Check(shieldFixture.Player.Statuses.Count == 1, "Reapplying Pressure Shield must refresh rather than stack.");
shieldFixture.Resolve("player", new DispelEffect(new()));
Check(shieldFixture.Player.Statuses.Count == 0, "Purge must remove Pressure Shield.");
shieldFixture = New(); shieldFixture.Player.Loadout.Focus = 1;
pressureShield = LearnSupport(shieldFixture, NexusRealms.Prelude.Database.Progression.Aquarius.PressureShield);
Check(!shieldFixture.Resolver.CanCommit("player", pressureShield, null)
    && !shieldFixture.Player.Loadout.Available(shieldFixture.Player.Loadout.Find(pressureShield.Id)!),
    "Pressure Shield must remain disabled without enough Focus for a nonzero duration.");

CarriedItem TestMelee(string id, DamageType type, int typed, int general = 1, WeaponRange range = WeaponRange.Short) =>
    new(id, id, "Right hand", Weapon: new WeaponData { IsMelee = true, Range = range,
        Damage = new Dictionary<DamageType, int> { [DamageType.Basic] = general, [type] = typed } });
Fixture Armed(params CarriedItem[] weapons) => new(definitions, CombatLoadout.FromState(weapons,
    weapons.Select((weapon, index) => (Slot: index == 0 ? "Right hand" : "Left hand", Id: weapon.Id.Value))
        .ToDictionary(pair => pair.Slot, pair => pair.Id), 4));
var spin = Armed(TestMelee("right", DamageType.Slashing, 3, general: 4));
AddWindCharacter(spin, "front-other", FormationSlot.FrontRight);
AddWindCharacter(spin, "middle", FormationSlot.MiddleLeft);
AddWindCharacter(spin, "back", FormationSlot.BackLeft);
var whirlwind = LearnSupport(spin, NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
Check(whirlwind.Targeting.Shape == TargetShape.FrontRow, "Whirlwind must declare front-row targeting.");
Check(spin.Resolver.CanCommit("player", whirlwind, null), "Whirlwind must select the front row without an anchor.");
var spinPreview = spin.Resolver.Preview("player", whirlwind, null);
Check(spinPreview.Characters["enemy"].Health == 13 && spin.Enemy.Health.Health == 20,
    "A single weapon must deal full normal damage, including Basic, in an isolated preview.");
var spinOutcome = spin.Resolver.Commit("player", whirlwind, null);
Check(spin.Enemy.Health.Health == 13 && spin.Resolver.Characters["front-other"].Health.Health == 13
    && spin.Resolver.Characters["middle"].Health.Health == 20 && spin.Resolver.Characters["back"].Health.Health == 20,
    "Whirlwind must hit every front-row enemy and leave other rows unchanged.");

spin = Armed(TestMelee("right", DamageType.Slashing, 3, general: 4), TestMelee("left", DamageType.Piercing, 2, general: 2));
whirlwind = LearnSupport(spin, NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
var dualPreview = spin.Resolver.Preview("player", whirlwind, null);
spinOutcome = spin.Resolver.Commit("player", whirlwind, null);
Check(spin.Enemy.Health.Health == 15 && dualPreview.Characters["enemy"].Health == 15
    && spinOutcome.Effects.All(effect => ((DamageEffect)effect.Requested).Type != DamageType.Basic)
    && spinOutcome.Effects.Select(effect => effect.Requested.AttackIndex).Distinct().Count() == 2,
    "Dual Whirlwind must attack once per weapon with only Basic removed, without halving typed damage.");
spin = Armed(TestMelee("right", DamageType.Slashing, 3), TestMelee("left", DamageType.Piercing, 2));
spin.Resolve("enemy", new ApplyEffect("pressure-shield", new(StatusClock.Timeline, 5)));
whirlwind = LearnSupport(spin, NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
spin.Resolver.Commit("player", whirlwind, null);
Check(spin.Enemy.Health.Health == 17, "Pressure Shield must reduce each of Whirlwind's two weapon attacks by one.");
spin = Armed(TestMelee("right", DamageType.Slashing, 3), TestMelee("left", DamageType.Piercing, 2, range: WeaponRange.Medium));
spin.Resolve("enemy", new ApplyEffect("elevated", new(StatusClock.Timeline, 5)));
whirlwind = LearnSupport(spin, NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
Check(spin.Resolver.CanCommit("player", whirlwind, null), "A longer melee weapon must allow Whirlwind to reach an elevated enemy.");
spin.Resolver.Commit("player", whirlwind, null);
Check(spin.Enemy.Health.Health == 18, "Only the longer melee weapon's attack must reach an elevated enemy.");
spin = Armed(TestMelee("right", DamageType.Slashing, 3));
spin.Resolve("enemy", new ApplyEffect("elevated", new(StatusClock.Timeline, 5)));
whirlwind = LearnSupport(spin, NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
Check(!spin.Resolver.CanCommit("player", whirlwind, null), "Whirlwind with only short melee must not target an elevated front row.");
spin = Armed(TestMelee("bow", DamageType.Piercing, 5) with { Weapon = new() { IsMelee = false, Range = WeaponRange.Long } });
whirlwind = LearnSupport(spin, NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
Check(!spin.Resolver.CanCommit("player", whirlwind, null), "Whirlwind must not use ranged weapons.");
var bewilderFixture = New();
var bewilder = LearnSupport(bewilderFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Bewilder);
Check(bewilder.TurnCost == 0.5f && bewilderFixture.Player.Loadout.Find(bewilder.Id)!.TurnCost == 0.5f,
    "Bewilder's runtime action and HUD command must use the authored half-turn cost.");
Check(!bewilderFixture.Resolver.CanCommit("player", bewilder, "player"), "Bewilder must target an enemy.");
var bewilderPreview = bewilderFixture.Resolver.Preview("player", bewilder, "enemy");
Check(bewilderPreview.Characters["enemy"].Statuses.Length == 1 && bewilderFixture.Enemy.Statuses.Count == 0,
    "Bewilder preview must apply its restriction without changing live statuses.");
bewilderFixture.Resolver.Commit("player", bewilder, "enemy");
Check(bewilderFixture.Combat.Timeline.CurrentTurn == 0.5f && bewilderFixture.Enemy.Statuses.Single().ExpiresAt == 2
    && bewilderFixture.Player.Loadout.Focus == 2,
    "Bewilder must cost half a turn, leave Focus unchanged and restrict for two full timeline turns.");
bewilderFixture.Combat.SubmitAction(new(1.5f, (_, _) => { }));
Check(bewilderFixture.Enemy.Statuses.Count == 0, "Bewilder must expire at its two-turn boundary.");

bewilderFixture = New();
var paidFocus = new EffectAction("spend-focus", new DamageEffect(DamageType.Basic, 1))
    { Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] };
bewilderFixture.Player.Loadout.Actions.Add(paidFocus.Id, paidFocus);
bewilderFixture.Player.Loadout.AddActionCommand(paidFocus);
bewilderFixture.Resolver.Register(paidFocus);
var directSpend = new EffectAction("direct-spend", new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 1) { Recipients = RecipientScope.Source });
var transferSpend = new EffectAction("transfer-spend", new TransferEffect("enemy", new ResourceSubject(ResourceKind.Focus), 1) { Recipients = RecipientScope.Source });
var involuntaryDrain = new EffectAction("drain-target", new ConsumeEffect(new ResourceSubject(ResourceKind.Focus), 1));
var freeDowse = LearnSupport(bewilderFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Dowse);
var focusCommand = new CommandDefinition("focus-command", "Focus command", 1, RequiresTarget: false, FocusCost: 1);
bewilderFixture.Player.Loadout.Commands.Add(focusCommand);
bewilderFixture.Resolve("player", new ApplyEffect("bewildered", new(StatusClock.Timeline, 1)));
Check(!bewilderFixture.Resolver.CanCommit("player", paidFocus, "enemy")
    && !bewilderFixture.Resolver.Allows("player", directSpend)
    && !bewilderFixture.Resolver.Allows("player", transferSpend)
    && !bewilderFixture.Player.Loadout.Available(bewilderFixture.Player.Loadout.Find(paidFocus.Id)!)
    && !bewilderFixture.Player.Loadout.Available(focusCommand),
    "Bewilder must block Focus costs, voluntary source consumption, transfers and legacy HUD commands.");
Check(bewilderFixture.Resolver.CanCommit("player", freeDowse, null)
    && bewilderFixture.Resolver.Allows("player", involuntaryDrain),
    "Bewilder must allow free Focus gain and must not grant immunity to Focus drain.");
var bewilderCandidates = CandidateGeneration.Enumerate(bewilderFixture.Resolver, "player", includeLayouts: false);
Check(!bewilderCandidates.Any(candidate => candidate.Action.Id is "spend-focus" or "focus-command")
    && bewilderCandidates.Any(candidate => candidate.Action.Id == freeDowse.Id),
    "AI candidates must share Bewilder's Focus restriction while retaining free actions.");
Reject(() => bewilderFixture.Resolver.Commit("player", paidFocus, "enemy"), "Blocked Focus commitment must fail.");
Check(bewilderFixture.Player.Loadout.Focus == 2, "Bewilder and rejected actions must not consume or erase existing Focus.");
bewilderFixture.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(bewilderFixture.Player.Statuses.Count == 0 && bewilderFixture.Resolver.CanCommit("player", paidFocus, "enemy")
    && bewilderFixture.Player.Loadout.Available(focusCommand),
    "Cleanse must exhaust Bewilder's one-turn duration and immediately restore Focus spending.");
bewilderFixture.Resolve("player", new ApplyEffect("bewildered", new(StatusClock.Timeline, 1)));
bewilderFixture.Resolve("player", new DispelEffect(new()));
Check(bewilderFixture.Resolver.CanCommit("player", paidFocus, "enemy"), "Purge must remove Bewilder's restriction.");

bewilderFixture = New();
bewilderFixture.Resolve("enemy", new ApplyEffect("bewildered", new(StatusClock.Timeline, 1)));
bewilderFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
bewilderFixture.Resolve("enemy", new ApplyEffect("bewildered", new(StatusClock.Timeline, 1)));
bewilderFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
Check(bewilderFixture.Enemy.Statuses.Count == 1 && bewilderFixture.Enemy.Statuses.Single().ExpiresAt == 1.5f,
    "Repeated Bewilder must refresh without stacking and survive the original expiration.");
bewilderFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
Check(bewilderFixture.Enemy.Statuses.Count == 0, "Refreshed Bewilder must expire at its new boundary.");
var stormFixture = New();
var initialCasterFocus = stormFixture.Player.Loadout.Focus;
var initialEnemyFocus = stormFixture.Enemy.Loadout.Focus;
var storm = LearnSupport(stormFixture, NexusRealms.Prelude.Database.Progression.Aquarius.EyeOfTheStorm);
var stormPreview = stormFixture.Resolver.Preview("player", storm, null);
Check(stormPreview.Characters["player"].Focus == initialCasterFocus && stormFixture.Player.Statuses.Count == 0
    && stormPreview.Schedule.Count(entry => entry.Label.StartsWith("Eye of the Storm:")) == 4,
    "Eye preview must schedule exactly four caster-only bonus events without changing live state.");
stormFixture.Resolver.Commit("player", storm, null);
var expectedStorm = new[] { 1, 1, 2, 3, 5, 8 };
var previousFocus = initialCasterFocus;
for (var boundary = 1; boundary <= 6; boundary++)
{
    if (boundary > 1) stormFixture.Combat.SubmitAction(new(1, (_, _) => { }));
    Check(stormFixture.Player.Loadout.Focus - previousFocus == expectedStorm[boundary - 1],
        $"Eye of the Storm must grant the expected Focus at boundary {boundary}.");
    Check(stormFixture.Enemy.Loadout.Focus == initialEnemyFocus + boundary,
        "Only the caster gets Eye bonuses; other characters get their normal +1 Focus.");
    previousFocus = stormFixture.Player.Loadout.Focus;
}
Check(stormFixture.Player.Statuses.Count == 0 && stormFixture.Player.Loadout.Focus == initialCasterFocus + 20,
    "Eye must finish with its final bonus and remove itself at the sixth boundary.");
stormFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(stormFixture.Player.Loadout.Focus == previousFocus + 1, "Normal Focus regeneration must continue after Eye finishes.");

stormFixture = New();
stormFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
Check(stormFixture.Player.Loadout.Focus == 2 && stormFixture.Enemy.Loadout.Focus == 0,
    "No Focus regeneration occurs at fractional action boundaries.");
storm = LearnSupport(stormFixture, NexusRealms.Prelude.Database.Progression.Aquarius.EyeOfTheStorm);
stormFixture.Resolver.Commit("player", storm, null);
Check(stormFixture.Player.Statuses.Single().ExpiresAt == 6, "A fractional cast must align Eye to the next six whole timeline boundaries.");
var forkStorm = stormFixture.Resolver.CopyForSimulation();
stormFixture.Combat.SubmitAction(new(1, (_, _) => { }));
forkStorm.Combat.SubmitAction(new(1, (_, _) => { })); forkStorm.Combat.Process();
Check(forkStorm.Characters["player"].Loadout.Focus == stormFixture.Player.Loadout.Focus
    && forkStorm.Characters["enemy"].Loadout.Focus == stormFixture.Enemy.Loadout.Focus,
    "Copying simulation state must neither duplicate regeneration nor mutate live scheduled bonuses.");
stormFixture.Resolve("player", new DispelEffect(new()));
Check(!stormFixture.Combat.Timeline.TurnOrder.Any(entry => entry.Label.StartsWith("Eye of the Storm:")),
    "Purge must remove Eye's pending bonus events from the timeline.");
var beforePurgedRecovery = stormFixture.Player.Loadout.Focus;
stormFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(stormFixture.Player.Loadout.Focus == beforePurgedRecovery + 1,
    "Purge must cancel Eye bonuses while preserving ordinary Focus regeneration.");
stormFixture = New();
stormFixture.Resolve("player", new ApplyEffect("eye-of-the-storm", new(StatusClock.Timeline, 6)));
stormFixture.Combat.SubmitAction(new(2, (_, _) => { }));
stormFixture.Resolve("player", new ApplyEffect("eye-of-the-storm", new(StatusClock.Timeline, 6)));
Check(stormFixture.Combat.Timeline.TurnOrder.Count(entry => entry.Label.StartsWith("Eye of the Storm:")) == 4,
    "Refresh must replace the four bonus events instead of leaving stale events on the timeline.");
var beforeRefreshBoundary = stormFixture.Player.Loadout.Focus;
stormFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(stormFixture.Player.Loadout.Focus == beforeRefreshBoundary + 1 && stormFixture.Player.Statuses.Count == 1,
    "Refreshing Eye must cancel old bonus callbacks and restart its sequence without stacking.");
var sight = New();
var visualAction = new EffectAction("aimed-spell", new DamageEffect(DamageType.Basic, 1))
{
    Targeting = new(TargetShape.Single),
    Flags = ActionFlags.Visual,
    Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)],
};
sight.Player.Loadout.Actions.Add(visualAction.Id, visualAction);
sight.Player.Loadout.AddActionCommand(visualAction);
sight.Resolver.Register(visualAction);
var selfDowse = LearnSupport(sight, NexusRealms.Prelude.Database.Progression.Aquarius.Dowse);
var blindFog = LearnSupport(sight, NexusRealms.Prelude.Database.Progression.Aquarius.Fog);
sight.Resolve("player", new ApplyEffect("blind", new(StatusClock.Timeline, 2)));
Check(!sight.Resolver.CanCommit("player", visualAction, "enemy")
    && !sight.Player.Loadout.Available(sight.Player.Loadout.Find(visualAction.Id)!)
    && !sight.Player.Loadout.Available(CombatLoadout.LeftHandStrike)
    && !sight.Player.Loadout.Available(CombatLoadout.RightHandStrike)
    && !sight.Player.Loadout.Available(CombatLoadout.Throw),
    "Blind must disable Visual capabilities, unarmed strikes, weapon strikes and throws in the shared HUD path.");
Check(sight.Resolver.CanCommit("player", selfDowse, null) && sight.Resolver.CanCommit("player", blindFog, null),
    "Blind must leave explicitly nonvisual self and all-enemy abilities usable.");
Check(sight.Player.Loadout.Find(visualAction.Id)!.Flags == ActionFlags.Visual
    && !new ActionFilter(RequiresActionFlags: ActionFlags.Visual).Matches(selfDowse, "player"),
    "Action flags must survive command adaptation independently of target shape.");
var sightCandidates = CandidateGeneration.Enumerate(sight.Resolver, "player", includeLayouts: false);
Check(!sightCandidates.Any(candidate => candidate.Action.Flags.HasFlag(ActionFlags.Visual))
    && sightCandidates.Any(candidate => candidate.Action.Id == blindFog.Id),
    "AI must exclude Visual actions under Blind while retaining nonvisual choices.");
Reject(() => sight.Resolver.Commit("player", visualAction, "enemy"), "Blind must reject a stale Visual commitment.");
Check(sight.Player.Loadout.Focus == 2, "A rejected Blind action must not pay Focus or timeline costs.");
sight.Resolver.ResolveCommitted(new EffectAction("incoming", new DamageEffect(DamageType.Basic, 1)), "enemy", ["player"]);
Check(sight.Player.Health.Health == 19, "Blind must not grant evasion or damage immunity.");
sight.Resolve("player", new DispelEffect(new()));
Check(sight.Resolver.CanCommit("player", visualAction, "enemy")
    && sight.Player.Loadout.Available(CombatLoadout.RightHandStrike), "Purge must restore Visual actions immediately.");
sight.Resolve("player", new ApplyEffect("blind", new(StatusClock.Timeline, 2)));
sight.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(sight.Player.Statuses.Single().ExpiresAt == 1, "Cleanse must shorten Blind's two-turn duration.");
sight.Combat.SubmitAction(new(1, (_, _) => { }));
Check(sight.Player.Statuses.Count == 0 && sight.Resolver.CanCommit("player", visualAction, "enemy"),
    "Shortened Blind must expire at its new timeline boundary and restore targeting.");

var fogFixture = New();
AddWindCharacter(fogFixture, "middle", FormationSlot.MiddleLeft);
AddWindCharacter(fogFixture, "back", FormationSlot.BackLeft);
AddWindCharacter(fogFixture, "friend", FormationSlot.FrontRight, CombatTeam.PlayerAndAllies);
var fog = LearnSupport(fogFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Fog);
var fogPreview = fogFixture.Resolver.Preview("player", fog, null);
Check(fogPreview.Outcome.Recipients.Length == 3 && fogPreview.Characters["enemy"].Statuses.Length == 1
    && fogFixture.Enemy.Statuses.Count == 0, "Fog preview must blind every enemy without changing live statuses.");
fogFixture.Resolver.Commit("player", fog, null);
Check(fogFixture.Resolver.Characters.Where(pair => pair.Value.Team == CombatTeam.Enemies)
    .All(pair => pair.Value.Statuses.Single().Definition.Id.Value == "blind" && pair.Value.Statuses.Single().ExpiresAt == 2)
    && fogFixture.Player.Statuses.Count == 0 && fogFixture.Resolver.Characters["friend"].Statuses.Count == 0,
    "Fog must blind all enemy rows for two timeline turns while preserving the caster and other-team characters.");
Check(fogFixture.Combat.Timeline.CurrentTurn == 1 && fogFixture.Enemy.Statuses.Count == 1,
    "Fog must leave one full turn of blindness after its one-turn casting cost.");
fogFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(fogFixture.Enemy.Statuses.Count == 0, "Fog's Blind must expire at the second timeline turn.");

var sandFixture = New();
AddWindCharacter(sandFixture, "middle", FormationSlot.MiddleLeft);
var sandstorm = LearnSupport(sandFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Sandstorm);
sandFixture.Resolve("enemy", new ApplyEffect("pressure-shield", new(StatusClock.Timeline, 5)));
var sandPreview = sandFixture.Resolver.Preview("player", sandstorm, null);
var sandResult = sandFixture.Resolver.Commit("player", sandstorm, null);
Check(sandFixture.Enemy.Health.Health == 20 && sandFixture.Enemy.Statuses.Any(status => status.Definition.Id.Value == "blind")
    && sandFixture.Resolver.Characters["middle"].Health.Health == 19
    && sandFixture.Resolver.Characters["middle"].Statuses.Single().Definition.Id.Value == "blind"
    && sandPreview.Characters["middle"].Health == 19,
    "Sandstorm must deal one Slashing damage and independently apply Blind even when the damage is resisted.");
Check(sandResult.Effects.Where(effect => effect.Verb == EffectVerb.Apply).All(effect => effect.ActualChange == 1),
    "Each non-evading enemy must receive Sandstorm's two-turn Blind effect.");
sandFixture = New(); Apply(sandFixture, "evade");
sandstorm = LearnSupport(sandFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Sandstorm);
var evadedSand = sandFixture.Resolver.Commit("player", sandstorm, null);
Check(sandFixture.Enemy.Health.Health == 20 && !sandFixture.Enemy.Statuses.Any(status => status.Definition.Id.Value == "blind")
    && evadedSand.Effects.All(effect => effect.Result == EffectResult.Evaded),
    "Evasion must protect against both Sandstorm operations under the existing rules.");

fogFixture = New();
fog = LearnSupport(fogFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Fog);
fogFixture.Resolver.Commit("player", fog, null);
fogFixture.Combat.SubmitAction(new(0.5f, (_, _) => { }));
fogFixture.Resolver.Commit("player", fog, null);
Check(fogFixture.Enemy.Statuses.Count == 1 && fogFixture.Enemy.Statuses.Single().ExpiresAt == 3.5f,
    "Recasting Fog must refresh Blind's duration without stacking or removing it at the old expiration.");
var blindCopy = fogFixture.Resolver.CopyForSimulation();
Check(!blindCopy.Allows("enemy", visualAction), "Simulation copies must retain Blind's targeting restriction.");
Reject(() => CombatRules.Validate(visualAction with { Flags = (ActionFlags)8 }, definitions),
    "Unknown action flags must be rejected.");
Reject(() => CombatRules.ValidateStatus(Status("bad-flags", new RestrictEffect(new(RequiresActionFlags: (ActionFlags)8)))),
    "Unknown action flags in action filters must be rejected.");
Reject(() => CombatRules.ValidateStatus(Status("bad-missing-flags", new RestrictEffect(new(MissingActionFlags: (ActionFlags)8)))),
    "Unknown missing action flags in action filters must be rejected.");
Check(NexusRealms.Prelude.Database.StatusEffects.Slow.Persistence.Duration == 3
    && NexusRealms.Prelude.Database.StatusEffects.Haste.Persistence.Duration == 3,
    "Shared Slow and Haste must default to three timeline turns.");
// Turn costs are fixed at commitment; status clocks and queued turns remain independent.
var tempo = New();
Apply(tempo, "slow", "player", new(StatusClock.Timeline, 10));
Check(tempo.Resolver.EffectiveTurnCost("player", 2) == 3, "Slow must default to 1.5 times the base Turn cost.");
Apply(tempo, "haste", "player", new(StatusClock.Timeline, 10));
Check(tempo.Resolver.EffectiveTurnCost("player", 2) == 2.25f, "Slow and Haste must multiply, including their default 0.75 Haste factor.");
Apply(tempo, "slow", "player", new(StatusClock.Timeline, 10));
Check(tempo.Resolver.EffectiveTurnCost("player", 2) == 2.25f, "Refreshing Slow must not add another multiplier.");
var tempoCopy = tempo.Resolver.CopyForSimulation();
Check(tempoCopy.EffectiveTurnCost("player", 2) == 2.25f, "Simulation copies must retain cost modifiers.");
Check(tempo.Player.Loadout.CommandCost(CombatLoadout.RightHandStrike)
    == tempo.Player.Loadout.BaseCommandCost(CombatLoadout.RightHandStrike) * 1.125f,
    "Command presentation must include each active multiplier once.");
tempo = New();
Apply(tempo, "slow", "player", new(StatusClock.Timeline, 0.5f));
var tempoAction = new ExampleAction("tempo", new DamageEffect(DamageType.Basic, 1)) { TurnCost = 2 };
tempo.Player.Loadout.Actions[tempoAction.Id] = tempoAction;
tempo.Resolver.Register(tempoAction);
var tempoPreview = tempo.Resolver.Preview("player", tempoAction, "enemy");
var tempoOutcome = tempo.Resolver.Commit("player", tempoAction, "enemy");
Check(tempoPreview.Outcome.Action.TurnCost == 3 && tempoOutcome.Action.TurnCost == 3
    && tempo.Combat.Timeline.CurrentTurn == 3 && tempoAction.TurnCost == 2,
    "Preview and commitment must agree on the scaled cost without changing authored data.");
Check(tempo.Player.Statuses.Count == 0 && tempo.Resolver.EffectiveTurnCost("player", 2) == 2,
    "Expiry during recovery must leave the committed turn intact and restore later costs.");
tempo = New();
Apply(tempo, "slow", "player", new(StatusClock.Timeline, 20));
var waitCandidate = CandidateGeneration.Enumerate(tempo.Resolver, "player", includeLayouts: false)
    .Single(candidate => candidate.Action.Id == CombatLoadout.Wait.Id.Value);
var waitOutcome = tempo.Resolver.Commit("player", waitCandidate);
Check(waitOutcome.Action.TurnCost == 10.01f
    && tempo.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "player").Turn == 10.01f,
    "Wait must continue to schedule just after the next character regardless of Slow.");
foreach (var invalidMultiplier in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
    Reject(() => CombatRules.ValidateStatus(Status("bad-tempo", new TurnCostMultiplierEffect(invalidMultiplier))),
        "Turn cost multipliers must be positive and finite.");
var headwindFixture = New();
var headwind = LearnSupport(headwindFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Headwind);
Check(!headwindFixture.Resolver.CanCommit("player", headwind, "player")
    && headwindFixture.Resolver.CanCommit("player", headwind, "enemy"),
    "Headwind must select a single enemy rather than its caster.");
Apply(headwindFixture, "blind", "player", new(StatusClock.Timeline, 2));
Check(!headwindFixture.Resolver.CanCommit("player", headwind, "enemy"), "Blind must block Headwind's visual targeting.");
headwindFixture.Resolve("player", new DispelEffect(new(Tag: "negative")));
var headwindPreview = headwindFixture.Resolver.Preview("player", headwind, "enemy");
Check(headwindPreview.Characters["enemy"].Statuses.Single().StartsWith("slow:", StringComparison.Ordinal)
    && headwindFixture.Enemy.Statuses.Count == 0,
    "Headwind preview must apply Slow without changing live state.");
headwindFixture.Resolver.Commit("player", headwind, "enemy");
Check(headwindFixture.Enemy.Statuses.Single().ExpiresAt == 3
    && headwindFixture.Resolver.EffectiveTurnCost("enemy", 2) == 3
    && headwindFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 10,
    "Headwind must scale future costs for three timeline turns without moving the queued enemy turn.");
headwindFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(headwindFixture.Enemy.Statuses.Count == 1, "Headwind must remain active for the caster's follow-up action.");
headwindFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(headwindFixture.Enemy.Statuses.Count == 0
    && headwindFixture.Resolver.EffectiveTurnCost("enemy", 2) == 2,
    "Headwind must expire at its exact timeline boundary.");
var hurricaneFixture = New();
AddWindCharacter(hurricaneFixture, "hurricane-middle", FormationSlot.MiddleLeft);
AddWindCharacter(hurricaneFixture, "hurricane-back", FormationSlot.BackLeft);
AddWindCharacter(hurricaneFixture, "hurricane-friend", FormationSlot.FrontRight, CombatTeam.PlayerAndAllies);
var hurricane = LearnSupport(hurricaneFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Hurricane);
Apply(hurricaneFixture, "blind", "player", new(StatusClock.Timeline, 2));
Check(hurricane is HurricaneAction && hurricaneFixture.Resolver.CanCommit("player", hurricane, null)
    && hurricaneFixture.Player.Loadout.Find(hurricane.Id) is { RequiresTarget: false },
    "Hurricane must be available without selecting a visual target, including while Blind.");
// Physical resistance cannot absorb either Basic or Electrical damage.
Apply(hurricaneFixture, "pressure-shield");
Apply(hurricaneFixture, "evade", "hurricane-back");
var hurricanePreview = hurricaneFixture.Resolver.Preview("player", hurricane, null);
Check(hurricanePreview.Outcome.Recipients.Length == 3
    && hurricanePreview.Characters["enemy"].Health == 17
    && hurricanePreview.Characters["hurricane-middle"].Health == 17
    && hurricanePreview.Characters["hurricane-back"].Health == 20
    && hurricaneFixture.Enemy.Health.Health == 20,
    "Hurricane preview must resolve damage and evasion independently across all enemy rows without mutating live health.");
var hurricaneOutcome = hurricaneFixture.Resolver.Commit("player", hurricane, null);
Check(hurricaneFixture.Enemy.Health.Health == 17
    && hurricaneFixture.Resolver.Characters["hurricane-middle"].Health.Health == 17
    && hurricaneFixture.Resolver.Characters["hurricane-back"].Health.Health == 20
    && hurricaneFixture.Resolver.Characters["hurricane-friend"].Health.Health == 20
    && hurricaneFixture.Player.Health.Health == 20 && hurricaneOutcome.Action.TurnCost == 1,
    "Hurricane must deal three total damage to unprotected enemies, preserve other teams, and match preview.");
var electricalWard = Status("electrical-ward", new NegateEffect(new(DamageType.Lightning)));
var hurricaneWardFixture = new Fixture(definitions.Append(new KeyValuePair<StatusEffectId, StatusEffectData>(electricalWard.Id, electricalWard))
    .ToDictionary(pair => pair.Key, pair => pair.Value));
Apply(hurricaneWardFixture, "electrical-ward");
hurricane = LearnSupport(hurricaneWardFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Hurricane);
var wardedHurricane = hurricaneWardFixture.Resolver.Commit("player", hurricane, null);
Check(hurricaneWardFixture.Enemy.Health.Health == 18
    && wardedHurricane.Effects.Single(effect => effect.Result == EffectResult.Negated).ResolvedAmount == 0,
    "Electrical negation must remove Hurricane's Electrical point while preserving its two Basic points.");
var silenceFixture = New();
var silence = LearnSupport(silenceFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Silence);
Check(!silenceFixture.Resolver.CanCommit("player", silence, "player")
    && silenceFixture.Resolver.CanCommit("player", silence, "enemy"), "Silence must target one enemy.");
var silencePreview = silenceFixture.Resolver.Preview("player", silence, "enemy");
Check(silencePreview.Characters["enemy"].Statuses.Single().StartsWith("silenced:", StringComparison.Ordinal)
    && silenceFixture.Enemy.Statuses.Count == 0, "Silence preview must isolate its two-turn restriction.");
silenceFixture.Resolver.Commit("player", silence, "enemy");
Check(silenceFixture.Enemy.Statuses.Single().ExpiresAt == 2, "Silence must last two timeline turns.");
silenceFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(silenceFixture.Enemy.Statuses.Count == 0, "Silence must expire at its exact timeline boundary.");
silenceFixture = New();
var magicalSelf = LearnSupport(silenceFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Dowse);
var magicalArea = LearnSupport(silenceFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Hurricane);
silence = LearnSupport(silenceFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Silence);
Apply(silenceFixture, "silenced", "player", new(StatusClock.Timeline, 2));
Check(!silenceFixture.Resolver.CanCommit("player", magicalSelf, null)
    && !silenceFixture.Resolver.CanCommit("player", magicalArea, null)
    && !silenceFixture.Resolver.CanCommit("player", silence, "enemy")
    && !silenceFixture.Player.Loadout.Available(silenceFixture.Player.Loadout.Find(magicalSelf.Id)!)
    && silenceFixture.Player.Loadout.Available(CombatLoadout.RightHandStrike),
    "Silence must block free magical self, area and visual actions in resolver and HUD while allowing ordinary attacks.");
Reject(() => silenceFixture.Resolver.Commit("player", magicalArea, null), "Silenced magical commitment must fail.");
Check(!CandidateGeneration.Enumerate(silenceFixture.Resolver, "player", includeLayouts: false)
    .Any(candidate => (candidate.Action.Flags & ActionFlags.Magical) != 0),
    "AI candidates must exclude silenced magical actions.");
Check(!silenceFixture.Resolver.CopyForSimulation().Allows("player", magicalArea), "Simulation copies must retain Silence.");
silenceFixture.Resolve("player", new DispelEffect(new(Tag: "negative"), DurationReduction: 1));
Check(silenceFixture.Player.Statuses.Single().ExpiresAt == 1, "Cleanse must shorten Silence.");
silenceFixture.Resolve("player", new DispelEffect(new()));
Check(silenceFixture.Resolver.CanCommit("player", magicalArea, null), "Purge must restore magical actions.");
Apply(silenceFixture, "blind", "player", new(StatusClock.Timeline, 2));
Check(silenceFixture.Resolver.CanCommit("player", magicalArea, null)
    && !silenceFixture.Resolver.CanCommit("player", silence, "enemy"), "Visual and Magical flags must remain independent.");
var whirlwindAction = AbilityActionRegistry.Default.Resolve(NexusRealms.Prelude.Database.Progression.Aquarius.Whirlwind);
Check(whirlwindAction.Targeting.Shape == TargetShape.FrontRow
    && whirlwindAction.Flags.HasFlag(ActionFlags.Melee)
    && !whirlwindAction.Flags.HasFlag(ActionFlags.Magical),
    "Whirlwind must remain a melee weapon action with independent front-row targeting.");
var barrierFixture = New();
var barrier = LearnSupport(barrierFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Barrier);
Check(!barrierFixture.Resolver.CanCommit("player", barrier, null), "Sound Barrier must be unavailable without a living back-row enemy.");
AddWindCharacter(barrierFixture, "barrier-middle", FormationSlot.MiddleLeft);
AddWindCharacter(barrierFixture, "barrier-left", FormationSlot.BackLeft);
AddWindCharacter(barrierFixture, "barrier-center", FormationSlot.BackCenter);
AddWindCharacter(barrierFixture, "barrier-right", FormationSlot.BackRight);
AddWindCharacter(barrierFixture, "barrier-friend", FormationSlot.BackRight, CombatTeam.PlayerAndAllies);
Apply(barrierFixture, "blind", "player", new(StatusClock.Timeline, 3));
Check(barrier is SoundBarrierAction && barrierFixture.Resolver.CanCommit("player", barrier, null)
    && barrierFixture.Player.Loadout.Find(barrier.Id) is { RequiresTarget: false },
    "Sound Barrier must require no visual target and remain usable while Blind.");
var barrierPreview = barrierFixture.Resolver.Preview("player", barrier, null);
Check(barrierPreview.Outcome.Recipients.Length == 3
    && barrierPreview.Characters["barrier-left"].Statuses.Length == 1
    && barrierFixture.Resolver.Characters["barrier-left"].Statuses.Count == 0,
    "Sound Barrier preview must isolate its back-row status applications.");
var barrierResult = barrierFixture.Resolver.Commit("player", barrier, null);
Check(barrierResult.Recipients.SequenceEqual(new[] { "barrier-center", "barrier-left", "barrier-right" })
    && barrierResult.Recipients.All(id => barrierFixture.Resolver.Characters[id].Statuses.Single().ExpiresAt == 2)
    && barrierFixture.Enemy.Statuses.Count == 0
    && barrierFixture.Resolver.Characters["barrier-middle"].Statuses.Count == 0
    && barrierFixture.Resolver.Characters["barrier-friend"].Statuses.Count == 0,
    "Sound Barrier must silence only enemies in all three back-row slots for two timeline turns.");
barrierFixture.Resolver.Commit("player", barrier, null);
Check(barrierResult.Recipients.All(id => barrierFixture.Resolver.Characters[id].Statuses.Count == 1
    && barrierFixture.Resolver.Characters[id].Statuses.Single().ExpiresAt == 3),
    "Sound Barrier must refresh Silence without stacking and survive the original expiry.");
barrierFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(barrierResult.Recipients.All(id => barrierFixture.Resolver.Characters[id].Statuses.Count == 0),
    "Refreshed Sound Barrier must expire at its exact timeline boundary.");
Apply(barrierFixture, "silenced", "player", new(StatusClock.Timeline, 2));
Check(!barrierFixture.Resolver.CanCommit("player", barrier, null), "Silence must block Sound Barrier's magical affinity.");
var twisterFixture = New();
AddWindCharacter(twisterFixture, "twister-middle", FormationSlot.MiddleLeft);
AddWindCharacter(twisterFixture, "twister-back", FormationSlot.BackLeft);
AddWindCharacter(twisterFixture, "twister-friend", FormationSlot.FrontRight, CombatTeam.PlayerAndAllies);
var twister = LearnSupport(twisterFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Twister);
Apply(twisterFixture, "blind", "player", new(StatusClock.Timeline, 2));
Apply(twisterFixture, "pressure-shield");
Apply(twisterFixture, "evade", "twister-back");
Check(twister is TwisterAction && twisterFixture.Resolver.CanCommit("player", twister, null)
    && twisterFixture.Player.Loadout.Find(twister.Id) is { RequiresTarget: false },
    "Twister must require no visual target and remain usable while Blind.");
var twisterPreview = twisterFixture.Resolver.Preview("player", twister, null);
Check(twisterPreview.Outcome.Recipients.Length == 3
    && twisterPreview.Characters["enemy"].Health == 20
    && twisterPreview.Characters["twister-middle"].Health == 19
    && twisterPreview.Characters["twister-back"].Health == 20
    && twisterPreview.Schedule.Single(turn => turn.CombatantId == "enemy").Turn == 10.5f
    && twisterFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 10,
    "Twister preview must isolate damage and delay, with resistance independent of Delay.");
var twisterResult = twisterFixture.Resolver.Commit("player", twister, null);
Check(twisterFixture.Enemy.Health.Health == 20
    && twisterFixture.Resolver.Characters["twister-middle"].Health.Health == 19
    && twisterFixture.Resolver.Characters["twister-back"].Health.Health == 20
    && twisterFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 10.5f
    && twisterFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "twister-middle").Turn == 10.5f
    && twisterFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "twister-back").Turn == 10
    && twisterFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "twister-friend").Turn == 10
    && twisterFixture.Resolver.Characters["twister-friend"].Health.Health == 20
    && twisterFixture.Player.Health.Health == 20 && twisterResult.Action.TurnCost == 1,
    "Twister must delay surviving non-evading enemies across all rows, preserve other teams, and match preview.");
Apply(twisterFixture, "silenced", "player", new(StatusClock.Timeline, 2));
Check(!twisterFixture.Resolver.CanCommit("player", twister, null), "Silence must block Twister's Magical flag.");
var shearFixture = New();
AddWindCharacter(shearFixture, "shear-center", FormationSlot.FrontCenter);
AddWindCharacter(shearFixture, "shear-right", FormationSlot.FrontRight);
AddWindCharacter(shearFixture, "shear-middle", FormationSlot.MiddleLeft);
AddWindCharacter(shearFixture, "shear-back", FormationSlot.BackLeft);
AddWindCharacter(shearFixture, "shear-friend", FormationSlot.FrontLeft, CombatTeam.PlayerAndAllies);
var shear = LearnSupport(shearFixture, NexusRealms.Prelude.Database.Progression.Aquarius.WindShear);
Apply(shearFixture, "blind", "player", new(StatusClock.Timeline, 5));
Check(shear is WindShearAction && shearFixture.Resolver.CanCommit("player", shear, null)
    && shearFixture.Player.Loadout.Find(shear.Id) is { RequiresTarget: false },
    "Wind Shear must need no visual target and remain usable while Blind.");
var shearPreview = shearFixture.Resolver.Preview("player", shear, null);
Check(shearPreview.Outcome.Recipients.SequenceEqual(new[] { "enemy", "shear-center", "shear-right" })
    && shearPreview.Characters["enemy"].Statuses.Length == 1 && shearFixture.Enemy.Statuses.Count == 0,
    "Wind Shear preview must isolate Slow applications to all three enemy front-row slots.");
shearFixture.Resolver.Commit("player", shear, null);
Check(shearPreview.Outcome.Recipients.All(id => shearFixture.Resolver.Characters[id].Statuses.Single().ExpiresAt == 3
    && shearFixture.Resolver.EffectiveTurnCost(id, 2) == 3
    && shearFixture.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == id).Turn == 10)
    && shearFixture.Resolver.Characters["shear-middle"].Statuses.Count == 0
    && shearFixture.Resolver.Characters["shear-back"].Statuses.Count == 0
    && shearFixture.Resolver.Characters["shear-friend"].Statuses.Count == 0,
    "Wind Shear must apply 1.5-times Slow for three turns without moving queued turns or affecting other rows and teams.");
var shearHeadwind = LearnSupport(shearFixture, NexusRealms.Prelude.Database.Progression.Aquarius.Headwind);
// Remove Blind so the Visual single-target spell can refresh the shared Slow status.
shearFixture.Resolve("player", new DispelEffect(new()));
shearFixture.Resolver.Commit("player", shearHeadwind, "enemy");
Check(shearFixture.Enemy.Statuses.Count == 1 && shearFixture.Enemy.Statuses.Single().ExpiresAt == 4
    && shearFixture.Resolver.EffectiveTurnCost("enemy", 2) == 3,
    "Headwind and Wind Shear must refresh the same Slow status rather than compound it.");
shearFixture.Combat.SubmitAction(new(1, (_, _) => { }));
Check(shearFixture.Resolver.Characters["shear-center"].Statuses.Count == 0 && shearFixture.Enemy.Statuses.Count == 1,
    "Wind Shear must expire exactly at turn three while refreshed Slow remains active.");
Apply(shearFixture, "silenced", "player", new(StatusClock.Timeline, 2));
Check(!shearFixture.Resolver.CanCommit("player", shear, null), "Silence must block Wind Shear's magical ability.");
var emptyShearFixture = New();
emptyShearFixture.Resolver.Characters["enemy"].Health.Health = 20;
var emptyShear = LearnSupport(emptyShearFixture, NexusRealms.Prelude.Database.Progression.Aquarius.WindShear);
Check(!emptyShearFixture.Resolver.CanCommit("player", emptyShear, null), "Wind Shear needs a living front-row enemy.");
Console.WriteLine("Action effects acceptance and lifecycle checks passed.");

sealed record TestAbilityAction(ActionId ActionId) : AbilityAction(ActionId)
{
    public override ImmutableArray<ActionEffect> Execute() => [new DamageEffect(DamageType.Basic, 1)];
}

/// <summary>A named action class can compose any ordered typed effects.</summary>
sealed record ExampleAction : EffectAction
{
    /// <summary>Creates a test capability with explicit effect order.</summary>
    public ExampleAction(string id, params ActionEffect[] effects) : base(id, effects) { }
}
/// <summary>Independent runtime owners and scheduler for each acceptance case.</summary>
sealed class Fixture
{
    /// <summary>Creates a player at Turn zero and an enemy scheduled at Turn ten.</summary>
    public Fixture(IReadOnlyDictionary<StatusEffectId, StatusEffectData> definitions, CombatLoadout? playerLoadout = null)
    {
        Combat.Add(new("player", true)); Combat.Add(new("enemy", false, 10, team: CombatTeam.Enemies));
        Resolver = new(Combat, definitions);
        Player = new("player", true, CombatTeam.PlayerAndAllies, new(20), playerLoadout ?? CombatLoadout.CreatePlayerLoadout());
        Enemy = new("enemy", false, CombatTeam.Enemies, new(20), new(), FormationSlot.FrontLeft);
        Resolver.Add(Player); Resolver.Add(Enemy);
        Combat.IsCombatantAlive = id => Resolver.Characters[id].Health.Health > 0;
        Combat.Process();
    }
    /// <summary>The fixture scheduler.</summary>
    public CombatSystem Combat { get; } = new();
    /// <summary>The shared effect resolver.</summary>
    public CombatActionResolver Resolver { get; }
    /// <summary>Existing player owners.</summary>
    public ActionCharacterState Player { get; }
    /// <summary>Existing enemy owners.</summary>
    public ActionCharacterState Enemy { get; }
    /// <summary>Evaluates an already committed effect sequence against one recipient.</summary>
    public ActionOutcome Resolve(string target, params ActionEffect[] effects) => Resolver.ResolveCommitted(new ExampleAction("test", effects), "player", [target]);
}
