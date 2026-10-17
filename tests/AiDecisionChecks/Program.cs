using System.Collections.Immutable;
using System.Text.Json;
using NexusRealms.Prelude;
using NexusRealms.Prelude.Actions;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void Reject(Action action, string message)
{
    try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; }
    throw new InvalidOperationException(message);
}
static ActionData Act(string id, params ActionEffect[] effects) => new() { Id = id, Effects = effects.ToImmutableArray(), Targeting = new(TargetShape.Single, TargetRelationship.OpposingTeam) };
static StatusEffectData Status(string id, params ActionEffect[] rules) => new()
{
    Id = id, Name = id, Description = id, Icon = new("unused"), Duplicates = DuplicateStatusRule.Refresh, Rules = rules.ToImmutableArray(),
};
static CharacterData Character(string id, StrategyAssignment? strategy = null) => new()
{
    Id = id, Name = id, Artwork = new("unused"), Portrait = new("unused"), Strategy = strategy ?? new(),
};
static string Json<T>(T value) => JsonSerializer.Serialize(value);

// Damage, stable ties, terminal objectives and safe immediate previews.
var resistance = Status("resist", new ResistEffect(new(DamageType.Slashing), 4));
var f = new Fixture([resistance]);
f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10);
f.Add("enemy", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft, ["resist"]);
var high = Act("a-high", new DamageEffect(DamageType.Slashing, 6));
var low = Act("b-low", new DamageEffect(DamageType.Basic, 3));
f.Give("enemy", high, low); f.Start();
var decision = CombatDecisionMaker.Decide(f.Resolver, "enemy");
Check(decision.Selected?.Action.Id == high.Id, "Resistance must be evaluated on the recipient, not the actor.");
// Put resistance on the player to reverse the winning base damage.
f.Resolver.ResolveCommitted(Act("apply", new ApplyEffect("resist", new())), "enemy", ["player"]);
decision = CombatDecisionMaker.Decide(f.Resolver, "enemy");
Check(decision.Selected?.Action.Id == low.Id, "Aggressive must choose actual damage after resistance.");
Check(Json(decision) == Json(CombatDecisionMaker.Decide(f.Resolver, "enemy")), "Repeated decisions and traces must be identical.");
var preview = f.Resolver.Preview("enemy", decision.Selected!);
Check(f.State("player").Health.Health == 20 && f.Combat.ActiveCombatant?.Id == "enemy", "Preview mutated live combat.");
f.Resolver.Commit("enemy", decision.Selected!);
Check(f.State("player").Health.Health == preview.Characters["player"].Health, "Preview/commitment Health mismatch.");

f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 1, 10); f.Add("enemy", false, CombatTeam.Enemies, 2, 0, FormationSlot.FrontLeft);
var kill = Act("kill", new DamageEffect(DamageType.Basic, 1));
var heal = Act("heal", new RestoreEffect(100)) with { Targeting = new(TargetShape.Self) };
f.Give("enemy", heal, kill); f.Combat.ConfigureObjectives([new(CombatConditionKind.AllEnemiesDefeated)], [new(CombatConditionKind.PlayerDefeated)]); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "enemy", new(StrategyId.Defensive)).Selected?.Action.Id == "kill", "Authored terminal objective must outrank healing.");

// Contrasting character policies and separate ally recipients.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10); f.Add("actor", false, CombatTeam.Enemies, 1, 0, FormationSlot.FrontLeft);
f.Add("ward", false, CombatTeam.Enemies, 1, 10, FormationSlot.FrontRight);
f.Give("player", Act("threat", new DamageEffect(DamageType.Basic, 4)));
heal = Act("restore", new RestoreEffect(4)) with { Targeting = new(TargetShape.Single, TargetRelationship.SameTeam) };
f.Give("actor", heal, Act("attack", new DamageEffect(DamageType.Basic, 1))); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Protective, ["ward"])).Selected?.TargetId == "ward", "Protective must preserve its ward.");
Check(CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Defensive)).Selected?.TargetId == "actor", "Defensive must preserve itself.");
Check(CombatDecisionMaker.Decide(f.Resolver, "actor").Candidates.Count(entry => entry.Candidate.Action.Id == "restore") == 2, "Each healing recipient must be enumerated.");
Check(f.Resolver.Preview("actor", heal, "actor").Characters["actor"].Health == 5, "Uncapped restoration must exceed initial Health.");
Reject(() => CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Protective)), "Protective scope must be explicit.");

// Irrelevant restrictions have no denial value; applicable ones deny observed legal opportunities.
var irrelevant = Status("irrelevant", new RestrictEffect(new(Classification: "unavailable")));
var applicable = Status("applicable", new RestrictEffect(new(Classification: "spell")));
f = new Fixture([irrelevant, applicable]); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("player", Act("spell", new DamageEffect(DamageType.Basic, 4)) with { Classifications = ["spell"] });
f.Give("actor", Act("bad", new ApplyEffect("irrelevant", new())), Act("good", new ApplyEffect("applicable", new()))); f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Disruptive));
Check(decision.Selected?.Action.Id == "good" && !decision.Candidates.Single(entry => entry.Candidate.Action.Id == "bad").Contributions.Any(entry => entry.Feature == "opposing-actions-denied"), "Restriction benefit must match real opposing actions.");

// Cost discipline, immediate impact, and identifiable elimination openings.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("actor", Act("economical", new DamageEffect(DamageType.Basic, 3)),
    Act("expensive", new DamageEffect(DamageType.Basic, 4)) with { Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] }); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "actor").Selected?.Action.Id == "expensive"
    && CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Cautious)).Selected?.Action.Id == "economical", "Cautious must preserve scarce resources when progress is adequate.");
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("actor", Act("safe", new DamageEffect(DamageType.Basic, 4)), Act("exposed", new DamageEffect(DamageType.Basic, 8),
    new DamageEffect(DamageType.Basic, 7) { Recipients = RecipientScope.Source })); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "actor").Selected?.Action.Id == "safe"
    && CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Reckless)).Selected?.Action.Id == "exposed", "Reckless must discount future personal contribution.");
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 10); f.Add("opening", false, CombatTeam.PlayerAndAllies, 1, 10, FormationSlot.FrontRight);
f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft); f.Give("actor", Act("pressure", new DamageEffect(DamageType.Basic, 15))); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "actor").Selected?.TargetId == "player"
    && CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Opportunistic)).Selected?.TargetId == "opening", "Opportunistic must favor an identifiable elimination opening over ordinary pressure.");

// Stable mode changes alter decisions; Reactive matches committed classifications.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 40, 10); f.Add("actor", false, CombatTeam.Enemies, 1, 0, FormationSlot.FrontLeft);
f.Give("player", Act("threat", new DamageEffect(DamageType.Basic, 4)));
f.Give("actor", Act("pressure", new DamageEffect(DamageType.Basic, 10)), Act("survive", new RestoreEffect(4)) with { Targeting = new(TargetShape.Self) });
f.Resolver.Behavior.Assign("actor", new(StrategyId.Chaotic) { Modes = [StrategyId.Aggressive, StrategyId.Defensive] }); f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "actor"); Check(decision.Selected?.Action.Id == "pressure", "Aggressive Chaos mode must apply offensive preferences.");
var clone = f.Resolver.CopyForSimulation(); clone.Commit("actor", decision.Selected!); clone.Combat.Process();
Check(CombatDecisionMaker.Decide(clone, "actor").Selected?.Action.Id == "survive", "Committed Chaos mode must switch policy without random choices.");
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 0); f.Add("actor", false, CombatTeam.Enemies, 20, 10, FormationSlot.FrontLeft);
var ranged = Act("observed-ranged", new DamageEffect(DamageType.Basic, 1)) with { Classifications = ["ranged"] };
f.Give("player", ranged); f.Give("actor", Act("attack", new DamageEffect(DamageType.Basic, 1))); f.Resolver.Behavior.Assign("actor", new(StrategyId.Reactive)); f.Start();
f.Resolver.Commit("player", ranged, "actor"); f.Resolver.Commit("player", ranged, "actor");
clone = f.Resolver.CopyForSimulation();
while (clone.Combat.ActiveCombatant?.Id != "actor") { clone.Combat.SubmitNoLegalAction(); clone.Combat.Process(); }
Check(CombatDecisionMaker.Decide(clone, "actor").Strategy == StrategyId.Defensive, "Reactive must match repeated committed ranged actions.");

// Analytical follows actual scheduled decisions and distinguishes retaliation from immediate impact.
var shield = Status("shield", new NegateEffect(new()));
f = new Fixture([shield]); f.Add("player", true, CombatTeam.PlayerAndAllies, 10, 1); f.Add("actor", false, CombatTeam.Enemies, 1, 0, FormationSlot.FrontLeft);
f.Give("player", Act("retaliate", new DamageEffect(DamageType.Basic, 2)) with { TurnCost = 2 });
f.Give("actor", Act("strike", new DamageEffect(DamageType.Basic, 3)) with { TurnCost = 2 },
    Act("protect", new ApplyEffect("shield", new(Uses: 1, UseEvent: StatusUseEvent.IncomingAction))) with { Targeting = new(TargetShape.Self), TurnCost = 2 });
f.Combat.ConfigureObjectives([new(CombatConditionKind.AllEnemiesDefeated)], [new(CombatConditionKind.PlayerDefeated)]); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Reckless)).Selected?.Action.Id == "strike", "Immediate Reckless must prefer impact.");
decision = CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Analytical) { DecisionDepth = 2, NodeBudget = 128, Fallback = StrategyId.Reckless });
Check(decision.Selected?.Action.Id == "protect" && decision.Planning is { UsedFallback: false, Truncated: false }, "Analytical must avoid forecast terminal failure through the real scheduler.");

// Supportive helps the allied group; Protective keeps an explicitly designated ward.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 10); f.Add("actor", false, CombatTeam.Enemies, 30, 0, FormationSlot.FrontLeft);
f.Add("a-ally", false, CombatTeam.Enemies, 1, 10, FormationSlot.FrontCenter); f.Add("z-ward", false, CombatTeam.Enemies, 1, 10, FormationSlot.FrontRight);
f.Give("player", Act("threat", new DamageEffect(DamageType.Basic, 4)));
f.Give("actor", Act("restore", new RestoreEffect(4)) with { Targeting = new(TargetShape.Single, TargetRelationship.SameTeam) }); f.Start();
Check(CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Supportive)).Selected?.TargetId == "a-ally"
    && CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Protective, ["z-ward"])).Selected?.TargetId == "z-ward", "Supportive and Protective must have distinct preservation scopes.");

// Delay is useful for crossed scheduled opportunities, not its raw authored magnitude.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 1); f.Add("actor", false, CombatTeam.Enemies, 30, 0, FormationSlot.FrontLeft);
f.Add("ally", false, CombatTeam.Enemies, 10, 2, FormationSlot.FrontCenter);
f.Give("actor", Act("a-modest", new DelayEffect(1.5f)), Act("z-large", new DelayEffect(10))); f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Disruptive));
Check(decision.Selected?.Action.Id == "a-modest" && decision.Candidates.All(entry => entry.Contributions.Single(part => part.Feature == "schedule-opportunities").Value == 2), "Equal scheduler opportunity denial must tie regardless of raw Delay, including scheduler ties.");

// A valid choice becomes stale when the live resource state changes; reevaluation pays only the replacement's cost.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("actor", Act("costly", new DamageEffect(DamageType.Basic, 4)) with { Costs = [new(new ResourceSubject(ResourceKind.Focus), 1)] },
    Act("free", new DamageEffect(DamageType.Basic, 1))); f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "actor"); f.State("actor").Loadout.Focus = 0;
decision = CombatDecisionMaker.CommitDecision(f.Resolver, decision);
Check(decision.Selected?.Action.Id == "free" && f.State("actor").Loadout.Focus == 1 && f.State("player").Health.Health == 49, "Stale selections must reevaluate without spending old costs; the next whole boundary restores one Focus.");

// Collection insertion order must not change tied action selection or structured traces.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 50, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
var tieA = Act("a", new DamageEffect(DamageType.Basic, 1)); var tieZ = Act("z", new DamageEffect(DamageType.Basic, 1));
f.Give("actor", tieZ, tieA); f.Start(); decision = CombatDecisionMaker.Decide(f.Resolver, "actor");
f.State("actor").Loadout.Actions.Clear(); f.Give("actor", tieA, tieZ);
Check(decision.Selected?.Action.Id == "a" && Json(decision) == Json(CombatDecisionMaker.Decide(f.Resolver, "actor")), "Canonical ties and traces must ignore action dictionary insertion order.");
f.Resolver.CaptureCandidateTraces = false;
var compact = CombatDecisionMaker.Decide(f.Resolver, "actor");
Check(compact.Selected == decision.Selected && compact.Candidates.Count == 0, "Disabling full candidate traces must not alter decisions.");

// Item-specific Throw choices, equipment ownership and next-hit use isolation.
var evade = Status("evade", new EvadeEffect(new()));
f = new Fixture([evade]); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 0, loadout: CombatLoadout.CreatePlayerLoadout());
f.Add("enemy", false, CombatTeam.Enemies, 20, 10, FormationSlot.FrontLeft); f.Start();
var throws = CandidateGeneration.Enumerate(f.Resolver, "player").Where(choice => choice.Kind == CandidateKind.Throw).ToArray();
Check(throws.Select(choice => choice.ItemId).ToHashSet().SetEquals(["starting-sword", "starting-rocks"]), "Throw choices must retain each legal item identity.");
var rock = throws.Single(choice => choice.ItemId == "starting-rocks");
preview = f.Resolver.Preview("player", rock);
Check(preview.Characters["player"].Items["starting-rocks"] == 4 && f.State("player").Loadout.Items.Single(item => item.Id.Value == "starting-rocks").Quantity == 5, "Preview consumed a live thrown item.");
f.Resolver.ResolveCommitted(Act("apply", new ApplyEffect("evade", new(Uses: 1, UseEvent: StatusUseEvent.IncomingAction))), "player", ["enemy"]);
preview = f.Resolver.Preview("player", rock);
Check(f.State("enemy").Statuses.Single().RemainingUses == 1 && preview.Characters["enemy"].Statuses.Length == 0, "Preview consumed a live next-hit status use.");

// Chaos advances on successful commitment only; memory is serializable and independent in a clone.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("actor", Act("attack", new DamageEffect(DamageType.Basic, 1))); f.Resolver.Behavior.Assign("actor", new(StrategyId.Chaotic)); f.Start();
var saved = Json(f.Resolver.Behavior.Save()); decision = CombatDecisionMaker.Decide(f.Resolver, "actor");
_ = f.Resolver.Preview("actor", decision.Selected!); _ = CombatDecisionMaker.Decide(f.Resolver, "actor");
Check(Json(f.Resolver.Behavior.Save()) == saved, "Preview or evaluation advanced Chaos memory.");
clone = f.Resolver.CopyForSimulation(); clone.Commit("actor", decision.Selected!);
Check(clone.Behavior.Snapshot("actor").Mode == 1 && f.Resolver.Behavior.Snapshot("actor").Mode == 0, "Committed Chaos must advance only isolated actor memory.");
var serialized = JsonSerializer.Deserialize<DecisionMemorySave>(Json(clone.Behavior.Save()))!;
clone.Behavior.Load(serialized); Check(clone.Behavior.Snapshot("actor").Mode == 1, "Saved Chaos cursor must survive JSON replay.");
Check(CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Reactive)).Strategy == StrategyId.Aggressive, "Reactive without history must use fallback.");

// Assignment transition conflicts, base changes under overrides, and latest-application precedence.
f.Resolver.Behavior.AddTransition("actor", new("z-low", new(StrategyConditionKind.HealthAtMost, Health: 20), new(StrategyId.Reckless)) { Priority = 1 });
f.Resolver.Behavior.AddTransition("actor", new("a-high", new(StrategyConditionKind.HealthAtMost, Health: 20), new(StrategyId.Defensive)) { Priority = 2 });
f.Resolver.Behavior.AddOverride("actor", "first", new(StrategyId.Supportive), 10, "test", commits: 1);
f.Resolver.Behavior.AddOverride("actor", "latest", new(StrategyId.Cautious), 10, "test", commits: 1);
f.Resolver.Behavior.ApplyBoundary();
Check(f.Resolver.Behavior.Snapshot("actor") is { Base.Strategy: StrategyId.Defensive, Effective.Strategy: StrategyId.Cautious }, "Transition must preserve the latest winning override.");
f.Resolver.Commit("actor", decision.Selected!);
Check(f.Resolver.Behavior.Snapshot("actor").Effective.Strategy == StrategyId.Defensive, "Override expiration must reveal changed base assignment.");
f.Resolver.Behavior.ApplyBoundary(); Check(f.Resolver.Behavior.Snapshot("actor").Base.Strategy == StrategyId.Defensive, "Continuously true losing transitions must not oscillate.");

// Scheduled events and status expiration in copied multi-decision state.
var timelineStatus = Status("temporary", new ResistEffect(new(), 1));
f = new Fixture([timelineStatus]); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 5); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("actor", Act("attack", new DamageEffect(DamageType.Basic, 1)));
f.Resolver.ResolveCommitted(Act("apply", new ApplyEffect("temporary", new(StatusClock.Timeline, 1))), "actor", ["player"]);
f.Resolver.ScheduleEvent(new(2, "known damage", "actor", "player", Act("event", new DamageEffect(DamageType.Basic, 3)))); f.Start();
clone = f.Resolver.CopyForSimulation(); clone.Commit("actor", CandidateGeneration.Enumerate(clone, "actor").First());
// Stop at each scheduled decision, advancing actor actions until the known event occurs.
for (var index = 0; index < 4 && clone.Combat.Timeline.CurrentTurn < 2; index++)
{
    clone.Combat.Process();
    if (clone.Combat.ActiveCombatant is { } active) clone.Combat.SubmitNoLegalAction();
}
Check(clone.Characters["player"].Statuses.Count == 0 && clone.Characters["player"].Health.Health == 17
    && f.State("player").Statuses.Count == 1 && f.State("player").Health.Health == 20, "Copied events or expiration mutated live owners.");
var analytic = new StrategyAssignment(StrategyId.Analytical) { DecisionDepth = 3, NodeBudget = 1 };
saved = CombatDecisionMaker.StateKey(f.Resolver);
decision = CombatDecisionMaker.Decide(f.Resolver, "actor", analytic);
Check(decision.Planning is { Truncated: true, UsedFallback: true, ExploredNodes: 1 }
    && CombatDecisionMaker.StateKey(f.Resolver) == saved, "Analytical budget must truncate deterministically without live mutation.");
Check(Json(decision) == Json(CombatDecisionMaker.Decide(f.Resolver, "actor", analytic)), "Truncated planning trace must replay deterministically.");

// Refresh keeps attribution order but stale expiration occurrences cannot remove the refreshed instance.
f = new Fixture([timelineStatus]); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
f.Give("actor", Act("attack", new DamageEffect(DamageType.Basic, 1)));
f.Resolver.ResolveCommitted(Act("first", new ApplyEffect("temporary", new(StatusClock.Timeline, 1))), "actor", ["player"]);
f.Resolver.ResolveCommitted(Act("refresh", new ApplyEffect("temporary", new(StatusClock.Timeline, 3))), "actor", ["player"]); f.Start();
clone = f.Resolver.CopyForSimulation(); clone.Commit("actor", CandidateGeneration.Enumerate(clone, "actor").First()); clone.Combat.Process();
Check(clone.Characters["player"].Statuses.Count == 1 && clone.Combat.Timeline.CurrentTurn == 1, "Stale expiration must not remove a refreshed status in simulation.");

// Explicit no-action scheduling, unsupported events, and character-owned configuration binding.
var restricted = Status("restricted", new RestrictEffect(new()));
f = new Fixture([restricted]); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft, ["restricted"]);
f.Give("actor", Act("attack", new DamageEffect(DamageType.Basic, 1))); f.Start();
decision = CombatDecisionMaker.DecideAndCommit(f.Resolver, "actor");
Check(decision.NoLegalAction && f.Combat.Timeline.CurrentTurn == 1, "NoLegalAction must use the defined advancing scheduler rule.");
f.Combat.ScheduleEvent(2, "opaque", _ => { });
Reject(() => f.Resolver.CopyForSimulation(), "Opaque event callbacks must fail explicitly.");
Reject(() => f.Resolver.ScheduleEvent(new(3, "hidden", "actor", "player", Act("hidden", new DamageEffect(DamageType.Basic, 1))) { KnownToAi = false }), "Hidden rules must not leak into exact previews.");
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 10); f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft); f.Add("ward", false, CombatTeam.Enemies, 20, 10, FormationSlot.FrontRight);
f.Resolver.ConfigureCharacters(new Dictionary<string, CharacterData> { ["actor"] = Character("guardian", new(StrategyId.Protective, ["protected-character"])), ["ward"] = Character("protected-character") });
Check(f.Resolver.Behavior.Snapshot("actor").Base.ProtectedIds!.Single() == "ward", "Character strategy references must bind independently of scenario configuration.");
Check(typeof(CharacterPlacement).GetProperties().All(property => property.PropertyType != typeof(StrategyAssignment)), "Scenario placements must not own strategies.");
// Shared objectives remain useful without strategy-specific capabilities or allies.
foreach (var strategy in new[] { StrategyId.Disruptive, StrategyId.Supportive })
foreach (var frontHealth in new[] { 2, 5 })
{
    f = new Fixture();
    f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 0, loadout: new CombatLoadout());
    f.Add("front", false, CombatTeam.Enemies, frontHealth, 10, FormationSlot.FrontLeft);
    f.Add("back", false, CombatTeam.Enemies, 20, 10, FormationSlot.BackLeft);
    f.Give("back", Act("ranged", new DamageEffect(DamageType.Basic, 3)));
    f.Start();
    decision = CombatDecisionMaker.Decide(f.Resolver, "player", new(strategy));
    Check(decision.Selected?.TargetId == "front" && decision.Selected.Action.Id != "wait",
        $"{strategy} must attack without specialized capabilities, despite exposing a new target.");
}

// The authored pistol user must reload and fire repeatedly instead of cycling layouts.
f = new Fixture();
f.Add("player", true, CombatTeam.PlayerAndAllies, 100, 100);
f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.BackLeft,
    loadout: CombatLoadout.FromCharacter(NexusRealms.Prelude.Database.Chapter1.Part1.AlleywayThug2));
f.Start();
for (var shot = 0; shot < 3; shot++)
{
    decision = CombatDecisionMaker.DecideAndCommit(f.Resolver, "actor");
    Check(decision.Selected?.Action.Id == "pistol-attack", "Loaded pistol must fire.");
    f.Combat.Process();
    var beforeReload = CombatDecisionMaker.StateKey(f.Resolver);
    decision = CombatDecisionMaker.Decide(f.Resolver, "actor");
    Check(decision.Selected?.Action.Id == "reload", "Empty pistol must reload rather than stow or equip.");
    Check(decision.Candidates.Single(entry => entry.Candidate.Key == decision.Selected!.Key)
        .Contributions.Any(entry => entry.Feature == "prepared-action-value" && entry.Value > 0),
        "Reload must be valued by its achievable follow-up.");
    Check(CombatDecisionMaker.StateKey(f.Resolver) == beforeReload, "Preparation probes must preserve live state.");
    CombatDecisionMaker.CommitDecision(f.Resolver, decision);
    f.Combat.Process();
}
Check(f.State("player").Health.Health == 91, "Three shots must actually resolve against the target.");
// Authored objectives outweigh incidental damage and follow the data when changed.
var resistSlash = Status("objective-resist-slash", new ResistEffect(new(DamageType.Slashing), 20));
var resistGeneral = Status("objective-resist-general", new ResistEffect(new(DamageType.Basic), 20));
foreach (var requiredId in new[] { "small-target", "large-target" })
{
    f = new Fixture([resistSlash, resistGeneral]);
    f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 0);
    f.Add("small-target", false, CombatTeam.Enemies, 6, 10, FormationSlot.FrontLeft, [resistSlash.Id]);
    f.Add("large-target", false, CombatTeam.Enemies, 100, 10, FormationSlot.FrontRight, [resistGeneral.Id]);
    f.Give("player", Act("a-high-damage", new DamageEffect(DamageType.Slashing, 10)),
        Act("z-low-damage", new DamageEffect(DamageType.Basic, 2)));
    f.Combat.ConfigureObjectives([new(CombatConditionKind.CharacterDefeated, requiredId)], [new(CombatConditionKind.PlayerDefeated)]);
    f.Start();
    var original = CombatDecisionMaker.StateKey(f.Resolver);
    decision = CombatDecisionMaker.Decide(f.Resolver, "player", new(StrategyId.Analytical));
    Check(decision.Selected?.TargetId == requiredId, "Analytical must follow the authored objective, independent of damage and candidate order.");
    Check(decision.Candidates.Any(entry => entry.Contributions.Any(feature => feature.Feature == "objective-victory-progress:0"
        && feature.Utility > 1000)), "Objective progress must carry a significant explicit weight.");
    Check(CombatDecisionMaker.StateKey(f.Resolver) == original
        && Json(decision) == Json(CombatDecisionMaker.Decide(f.Resolver, "player", new(StrategyId.Analytical))),
        "Beam planning must preserve live state and replay deterministically.");
    var otherId = requiredId == "small-target" ? "large-target" : "small-target";
    f.Combat.ConfigureObjectives([new(CombatConditionKind.CharacterDefeated, otherId)], [new(CombatConditionKind.PlayerDefeated)]);
    Check(CombatDecisionMaker.StateKey(f.Resolver) != original, "Changing objective data must invalidate a saved decision.");    Reject(() => new StrategyAssignment(StrategyId.Analytical) { BeamWidth = 0 }.Validate(), "Invalid beam widths must be rejected.");
}

// Uncapped surplus healing must not manufacture progress toward an objective.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 0);
f.Add("target", false, CombatTeam.Enemies, 50, 100, FormationSlot.FrontLeft);
f.Give("player", Act("surplus", new RestoreEffect(100)) with { Targeting = new(TargetShape.Self) });
f.Combat.ConfigureObjectives([new(CombatConditionKind.AllEnemiesDefeated)], [new(CombatConditionKind.PlayerDefeated)]);
f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "player", new(StrategyId.Supportive));
Check(decision.Candidates.All(entry => !entry.Contributions.Any(feature => feature.Feature.StartsWith("objective-", StringComparison.Ordinal))),
    "Surplus healing must retain ordinary preservation value without an objective progress windfall.");
// A truncated deeper layer must retain the fully compared earlier layer.
f = new Fixture(); f.Add("player", true, CombatTeam.PlayerAndAllies, 20, 0);
f.Add("target", false, CombatTeam.Enemies, 50, 100, FormationSlot.FrontLeft);
f.Give("player", Act("a-weak", new DamageEffect(DamageType.Basic, 1)), Act("z-strong", new DamageEffect(DamageType.Basic, 3)));
f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "player", new(StrategyId.Analytical) { DecisionDepth = 3, NodeBudget = 6 });
Check(decision.Selected?.Action.Id == "z-strong"
    && decision.Planning is { Truncated: true, UsedFallback: false, CompletedDepth: 1 },
    "Budget exhaustion must preserve a completed comparison rather than favor a candidate prefix.");
// Every enemy policy pursues Defeat directly, with a separate player Victory condition.
foreach (var strategy in Enum.GetValues<StrategyId>())
{
    f = new Fixture();
    f.Add("player", true, CombatTeam.PlayerAndAllies, 10, 100);
    f.Add("actor", false, CombatTeam.Enemies, 20, 0, FormationSlot.FrontLeft);
    f.Add("ward", false, CombatTeam.Enemies, 1, 100, FormationSlot.FrontRight);
    f.Give("actor", Act("a-protect-victory-target", new RestoreEffect(1)) with { Targeting = new(TargetShape.Single, TargetRelationship.SameTeam) },
        Act("z-pursue-defeat", new DamageEffect(DamageType.Basic, 1)));
    f.Combat.ConfigureObjectives([new(CombatConditionKind.CharacterDefeated, "ward")], [new(CombatConditionKind.PlayerDefeated)]);
    f.Start();
    var assignment = new StrategyAssignment(strategy) { ProtectedIds = ["ward"] };
    decision = CombatDecisionMaker.Decide(f.Resolver, "actor", assignment);
    Check(decision.Selected?.Action.Id == "z-pursue-defeat", $"{strategy} must pursue authored Defeat rather than merely impede Victory.");
    Check(decision.Candidates.Single(entry => entry.Candidate.Key == decision.Selected!.Key).Contributions
        .Any(feature => feature.Feature == "objective-defeat-progress:0" && feature.Utility > 0),
        $"{strategy} must receive positive progress from the enemy's own objective.");
}

// A known timeline event can fulfill Defeat without the enemy dealing direct damage.
f = new Fixture();
f.Add("player", true, CombatTeam.PlayerAndAllies, 10, 1);
f.Add("actor", false, CombatTeam.Enemies, 1, 0, FormationSlot.FrontLeft);
f.Give("player", Act("finish", new DamageEffect(DamageType.Basic, 1)));
f.Give("actor", Act("a-attack", new DamageEffect(DamageType.Basic, 3)) with { TurnCost = 2 },
    Act("z-stall", new DelayEffect(2)) with { TurnCost = 2 });
f.Combat.ConfigureObjectives([new(CombatConditionKind.AllEnemiesDefeated)], [new(CombatConditionKind.PlayerDefeated)]);
f.Resolver.ScheduleEvent(new(1.5f, "known hazard", "actor", "player", Act("hazard", new DamageEffect(DamageType.Basic, 10))));
f.Start();
decision = CombatDecisionMaker.Decide(f.Resolver, "actor", new(StrategyId.Analytical));
Check(decision.Selected?.Action.Id == "z-stall"
    && decision.Candidates.Single(entry => entry.Candidate.Key == decision.Selected!.Key).Preview.EncounterOutcome == CombatOutcome.Defeat,
    "Enemy Analytical must stall for a known Defeat-producing event instead of maximizing direct damage.");
Console.WriteLine("AI decision acceptance checks passed.");

sealed class Fixture
{
    public CombatSystem Combat { get; } = new();
    public CombatActionResolver Resolver { get; }
    public Fixture(StatusEffectData[]? statuses = null) => Resolver = new(Combat, (statuses ?? []).ToDictionary(status => status.Id));
    public void Add(string id, bool player, CombatTeam team, int health, float turn, FormationSlot? position = null,
        string[]? statuses = null, CombatLoadout? loadout = null)
    {
        Combat.Add(new(id, player, turn, team: team));
        // FromCharacter creates only the declared repertoire; fixtures never acquire invented hand actions.
        loadout ??= CombatLoadout.FromCharacter(new CharacterData { Id = id, Name = id, Artwork = new("unused"), Portrait = new("unused"), Actions = [] });
        var state = new ActionCharacterState(id, player, team, new(health), loadout, position);
        Resolver.Add(state, statuses?.Select(value => new StatusEffectId(value)));
        state.Health.DamageTaken += result => { if (result.IsLethal) Combat.Remove(id); };
        Combat.IsCombatantAlive = key => Resolver.Characters[key].Health.Health > 0;
    }
    public ActionCharacterState State(string id) => Resolver.Characters[id];
    public void Give(string id, params ActionData[] actions)
    {
        foreach (var action in actions) { State(id).Loadout.Actions[action.Id] = action; Resolver.Register(action); }
    }
    public void Start() => Combat.Process();
}



