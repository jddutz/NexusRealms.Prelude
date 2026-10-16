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
var evade = Status("evade", new EvadeEffect(new()));
var restriction = Status("restriction", new RestrictEffect(new(Classification: "weapon")));
var modifier = Status("modifier", new ModifyEffect(new(EffectVerb.Damage), NumericalField.DamageAmount, 2));
var definitions = new[] { resistance, negate, evade, restriction, modifier,
    Status("reject") with { Duplicates = DuplicateStatusRule.Reject },
    Status("replace") with { Duplicates = DuplicateStatusRule.Replace },
    Status("stack") with { Duplicates = DuplicateStatusRule.Stack } }.ToDictionary(status => status.Id);

Fixture New() => new(definitions);
void Apply(Fixture f, string id, string recipient = "enemy", StatusPersistence? persistence = null)
    => f.Resolve(recipient, new ApplyEffect(id, persistence ?? new()));

var aiFixture = New(); Apply(aiFixture, "resistance");
var mitigated = new ExampleAction("a-mitigated", new DamageEffect(DamageType.Slashing, 6));
var effective = new ExampleAction("b-effective", new DamageEffect(DamageType.General, 6))
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
Check(aiFixture.Enemy.Health.Health == preview.Characters["enemy"].Health && aiFixture.Player.Loadout.Focus == preview.Characters["player"].Focus,
    "Known immediate preview must match committed health and Focus.");
Console.WriteLine("AI preview and deterministic selection checks passed.");
var mixed = new ActionEffect[] { new DamageEffect(DamageType.General, 1), new DamageEffect(DamageType.Slashing, 2), new DamageEffect(DamageType.Fire, 1) };
var f = New(); Apply(f, "resistance");
var outcome = f.Resolve("enemy", mixed);
Check(f.Enemy.Health.Health == 17 && outcome.Effects.Sum(effect => effect.ResolvedAmount) == 3, "Typed resistance acceptance case.");
f = New(); Apply(f, "negate");
outcome = f.Resolve("enemy", mixed.Concat([new DelayEffect(2)]).ToArray());
Check(f.Enemy.Health.Health == 17 && f.Combat.Timeline.TurnOrder.Single(turn => turn.CombatantId == "enemy").Turn == 12,
    "Negation must preserve nonmatching damage and non-damage effects.");
f = New(); Apply(f, "evade");
var costly = new ExampleAction("costly", new DamageEffect(DamageType.General, 3), new DelayEffect(2), new ApplyEffect("resistance", new()))
    { Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] };
f.Player.Loadout.Actions[costly.Id] = costly; f.Resolver.Register(costly);
outcome = f.Resolver.Commit("player", costly, "enemy");
Check(outcome.Effects.All(effect => effect.Result == EffectResult.Evaded) && f.Player.Loadout.Focus == 1
    && f.Enemy.Health.Health == 20 && f.Enemy.Statuses.Count == 1, "Evasion must cancel every target effect and retain costs.");
f = New(); Apply(f, "evade");
var otherHealth = new DamageReceiver(20);
f.Combat.Add(new("other", false, 10, team: CombatTeam.Enemies));
f.Resolver.Add(new("other", false, CombatTeam.Enemies, otherHealth, new(), FormationSlot.FrontRight));
var area = new ExampleAction("area", new DamageEffect(DamageType.General, 3), new DelayEffect(2))
    { Targeting = new(TargetShape.All, TargetRelationship.OpposingTeam) };
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
var weapon = new ExampleAction("weapon", new DamageEffect(DamageType.General, 1)) { Classifications = ["weapon"] };
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
outcome = f.Resolve("enemy", new DamageEffect(DamageType.General, 3));
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
f.Resolve("enemy", new DamageEffect(DamageType.General, 3));
Check(f.Enemy.Statuses.Count == 0 && f.Enemy.Health.Health == 20, "Declared incoming-action use includes an evaded action.");
f = New(); Apply(f, "evade", "enemy", new(Uses: 1, UseEvent: StatusUseEvent.NonEvadedIncomingAction));
f.Resolve("enemy", new DamageEffect(DamageType.General, 3));
Check(f.Enemy.Statuses.Single().RemainingUses == 1, "Declared non-evaded use must exclude evasion.");
f = New();
var unlock = new ExampleAction("unlocked", new DamageEffect(DamageType.General, 1))
    { Requirements = new() { Equipment = [new("Right hand")] } };
var ability = new AbilityData { Id = "progression", Name = "Progression", Description = "Test", Icon = new("unused"), Actions = [unlock] };
f.Player.Loadout.Learn(ability); var learned = f.Player.Loadout.Actions[unlock.Id]; f.Resolver.Register(learned);
Check(f.Resolver.CanCommit("player", learned, "enemy"), "Ability and equipped item unlock an action.");
f.Player.Loadout.Learn(ability);
Check(f.Player.Loadout.Commands.Count(command => command.Id.Value == unlock.Id) == 1
    && f.Player.Loadout.Actions[unlock.Id].Requirements.Abilities.Count(id => id == ability.Id) == 1,
    "Repeated learning must not duplicate commands or prerequisites.");
var conflictingAbility = ability with { Id = "conflicting" };
Reject(() => f.Player.Loadout.Learn(conflictingAbility), "Conflicting unlocks must be rejected.");
Check(!f.Player.Loadout.LearnedAbilities.Contains(conflictingAbility.Id), "Rejected learning must not mutate progression.");
var committedUnlock = f.Resolver.Commit("player", learned, "enemy");
Check(f.Enemy.Health.Health == 19 && committedUnlock.Action.Id == unlock.Id
    && committedUnlock.Effects.Single().ActualChange == 1, "Learned capability must resolve through shared combat effects.");
f.Resolve("player", new RemoveEffect(new RemoveEquipment("Right hand")));
Check(!f.Resolver.CanCommit("player", learned, "enemy") && f.Player.Loadout.LearnedAbilities.Contains(ability.Id), "Equipment loss disables action without unlearning progression.");
Reject(() => f.Resolver.Register(new ExampleAction("negative", new DamageEffect(DamageType.General, -1))), "Negative effect validation.");
Reject(() => f.Resolver.Register(new ExampleAction("missing", new ApplyEffect("unknown", new()))), "Unresolved status validation.");
Reject(() => CombatRules.Persistence(new(StatusClock.RecipientTurnStart, 1.5f)), "Ambiguous duration validation.");
Reject(() => CombatRules.ValidateStatus(Status("invalid", new ModifyEffect(new(EffectVerb.Negate), NumericalField.DamageAmount, 2))), "Unsupported numerical field validation.");
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
var frozen = new ExampleAction("frozen-row", new MoveEffect(FormationSlot.MiddleLeft), new DamageEffect(DamageType.General, 1))
    { Targeting = new(TargetShape.Row, TargetRelationship.OpposingTeam) };
f.Player.Loadout.Actions[frozen.Id] = frozen; f.Resolver.Register(frozen);
outcome = f.Resolver.Commit("player", frozen, "enemy");
Check(f.Enemy.Health.Health == 19 && rowOther.Health == 19 && outcome.Recipients.Length == 2, "Movement must not recalculate an area's recipients.");
var filter = new DamageFilter(DamageType.Fire, "source");
Check(filter.Matches(DamageType.Fire, "source") && !filter.Matches(DamageType.Fire, "other")
    && !filter.Matches(DamageType.Ice, "source"), "Source-and-type filters default to both criteria.");
f = New(); f.Enemy.Health.Health = 10;
f.Resolve("enemy", new RestoreEffect(2), new DamageEffect(DamageType.General, 1));
var finalHealth = f.Enemy.Health.Health;
var replay = New(); replay.Enemy.Health.Health = 10;
replay.Resolve("enemy", new RestoreEffect(2), new DamageEffect(DamageType.General, 1));
Check(finalHealth == 11 && replay.Enemy.Health.Health == finalHealth, "Authored order and same-state deterministic resolution.");
Check(!typeof(CharacterData).Assembly.GetTypes().Where(type => type.Namespace == typeof(CharacterData).Namespace)
    .SelectMany(type => type.GetProperties()).Any(property => property.PropertyType.Namespace is { } ns && ns.StartsWith("NexusRealms.Prelude.Combat", StringComparison.Ordinal)),
    "DataModel must not expose combat runtime classes.");
var healingTarget = new DamageReceiver(18);
Check(HealingRules.Restore(healingTarget, 5, 20) == 2 && healingTarget.Health == 20,
    "Shared healing must cap at maximum Health and report the actual increase.");
Check(HealingRules.Restore(healingTarget, 5, 15) == 0 && healingTarget.Health == 20,
    "Healing must never reduce Health already above its cap.");
healingTarget.Health = 0;
Check(HealingRules.Restore(healingTarget, 10, 20) == 0 && healingTarget.Health == 0,
    "Healing must not resurrect defeated characters.");
healingTarget.Health = int.MaxValue - 1;
Check(HealingRules.Restore(healingTarget, double.MaxValue) == 1 && healingTarget.Health == int.MaxValue,
    "Healing must saturate without integer overflow.");
Reject(() => HealingRules.Restore(healingTarget, double.NaN), "Nonfinite healing must be rejected.");
Console.WriteLine("Action effects acceptance and lifecycle checks passed.");

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
    public Fixture(IReadOnlyDictionary<StatusEffectId, StatusEffectData> definitions)
    {
        Combat.Add(new("player", true)); Combat.Add(new("enemy", false, 10, team: CombatTeam.Enemies));
        Resolver = new(Combat, definitions);
        Player = new("player", true, CombatTeam.PlayerAndAllies, new(20), CombatLoadout.CreatePlayerLoadout());
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
