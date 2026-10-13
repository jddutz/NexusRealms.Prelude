using NexusRealms.Prelude.Actions;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Combat;

public sealed record PreviewCharacter(int Health, int Focus, FormationSlot? Position,
    ImmutableSortedDictionary<string, int> Items, ImmutableArray<string> Actions, ImmutableArray<string> Statuses,
    ImmutableSortedDictionary<string, string> Equipment, ImmutableSortedDictionary<string, int> Shots, int TurnsStarted,
    ImmutableSortedDictionary<string, string> Layout);
public sealed record ActionPreview(ActionOutcome Outcome, IReadOnlyDictionary<string, PreviewCharacter> Characters,
    CombatOutcome? EncounterOutcome, ImmutableArray<TimelineOccurrence> Schedule)
{
    internal static ActionPreview Capture(ActionOutcome outcome, CombatActionResolver resolver) => new(outcome,
        resolver.Characters.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToImmutableSortedDictionary(pair => pair.Key,
            pair => new PreviewCharacter(pair.Value.Health.Health, pair.Value.Loadout.Focus, pair.Value.Position,
                pair.Value.Loadout.Items.ToImmutableSortedDictionary(item => item.Id.Value, item => item.Quantity, StringComparer.Ordinal),
                pair.Value.Loadout.Actions.Keys.Order(StringComparer.Ordinal).ToImmutableArray(),
                pair.Value.Statuses.OrderBy(status => status.Sequence).Select(status => FormattableString.Invariant($"{status.Definition.Id}:{status.Sequence}:{status.RemainingUses}:{status.ExpiresAt}:{status.ExpiresOnTurnStart}")).ToImmutableArray(),
                pair.Value.Loadout.Equipment.ToImmutableSortedDictionary(StringComparer.Ordinal),
                pair.Value.Loadout.Items.Where(item => item.Weapon?.Shots > 0).ToImmutableSortedDictionary(item => item.Id.Value, item => pair.Value.Loadout.ShotsRemaining(item), StringComparer.Ordinal),
                pair.Value.TurnsStarted, pair.Value.Loadout.CopyForPreview().BeginItemLayout().ToImmutableSortedDictionary(StringComparer.Ordinal)), StringComparer.Ordinal),
        resolver.Combat.Outcome, resolver.Combat.Timeline.TurnOrder.ToImmutableArray());
}
public sealed record ScoreContribution(string Feature, long Value, int Weight, long Utility);
public sealed record CandidateEvaluation(ActionCandidate Candidate, ActionPreview Preview, long Utility,
    int ObjectiveRank, ImmutableArray<ScoreContribution> Contributions);
public sealed record PlanningTrace(int Depth, int Budget, int ExploredNodes, bool Truncated, bool UsedFallback,
    StrategyId AlliedContinuation, StrategyId OpposingContinuation);
public sealed record DecisionTrace(string ActorId, StrategyId Strategy, IReadOnlyList<CandidateEvaluation> Candidates,
    ActionCandidate? Selected, string[] Limitations)
{
    public bool NoLegalAction => Selected is null;
    public float Turn { get; init; }
    public BehaviorSnapshot? Behavior { get; init; }
    public ImmutableArray<RejectedCandidate> Rejected { get; init; } = [];
    public ImmutableArray<string> Ties { get; init; } = [];
    public PlanningTrace? Planning { get; init; }
    public string Observation { get; init; } = "Fully visible combat; no unrevealed events or future player input.";
    public string Objective { get; init; } = "Authored player victory/defeat conditions; enemy side reverses their terminal preference.";
    public string StateKey { get; init; } = "";
}
internal sealed record ThreatFacts(ImmutableDictionary<string, int> Harm, ImmutableHashSet<string> Actions, ImmutableHashSet<string> AlliedActions);

/// <summary>Complete legal actions, shared resolution, integer utility and canonical ties.</summary>
public static class CombatDecisionMaker
{
    /// <summary>Stores candidate scores without requiring a full preview when traces are disabled.</summary>
    /// <param name="Candidate">The legal action candidate.</param>
    /// <param name="Preview">The resulting state, when candidate traces are requested.</param>
    /// <param name="Utility">The weighted utility score.</param>
    /// <param name="ObjectiveRank">The terminal-outcome preference rank.</param>
    /// <param name="Contributions">The feature breakdown, when candidate traces are requested.</param>
    private sealed record ScoredCandidate(ActionCandidate Candidate, ActionPreview? Preview, long Utility,
        int ObjectiveRank, ImmutableArray<ScoreContribution> Contributions);

    public static DecisionTrace Decide(CombatActionResolver resolver, string actorId, StrategyAssignment? assignment = null)
    {
        if (resolver.Combat.ActiveCombatant?.Id != actorId) throw new InvalidOperationException("AI requires the actor's decision boundary.");
        var snapshot = resolver.Behavior.Snapshot(actorId, assignment);
        var effective = Effective(resolver, actorId, snapshot);
        var rejected = resolver.CaptureCandidateTraces ? new List<RejectedCandidate>() : null;
        var candidates = CandidateGeneration.Enumerate(resolver, actorId, rejected);
        var evaluations = new List<ScoredCandidate>();
        PlanningTrace? planning = null;
        if (effective == StrategyId.Analytical)
            planning = Plan(resolver, actorId, snapshot, candidates, evaluations);
        else
        {
            var threatCache = new Dictionary<string, ThreatFacts>(StringComparer.Ordinal);
            var beforeThreat = Threats(resolver, actorId, threatCache);
            foreach (var candidate in candidates)
            {
                var copy = resolver.CopyForSimulation();
                var outcome = copy.Commit(actorId, candidate);
                var afterThreat = Threats(copy, actorId, threatCache);
                evaluations.Add(Evaluate(resolver, copy, actorId, candidate, outcome, snapshot, effective,
                    beforeThreat, afterThreat, resolver.CaptureCandidateTraces));
            }
        }
        var ordered = evaluations.OrderByDescending(entry => entry.ObjectiveRank).ThenByDescending(entry => entry.Utility).ToArray();
        var selected = ordered.FirstOrDefault();
        var candidateTraces = resolver.CaptureCandidateTraces
            ? evaluations.Select(entry => new CandidateEvaluation(entry.Candidate,
                entry.Preview ?? throw new InvalidOperationException("Candidate trace preview was not captured."),
                entry.Utility, entry.ObjectiveRank, entry.Contributions)).ToArray()
            : Array.Empty<CandidateEvaluation>();
        var trace = new DecisionTrace(actorId, effective, candidateTraces, selected?.Candidate,
            ["Local threat estimate: each fully observed opposing action could occur now; no intervening events or player choice are predicted.",
             "Protection uses worst Health loss per recipient; enabled and denied actions use current legal choices. Analytical follows scheduled events."])
        {
            Turn = resolver.Combat.Timeline.CurrentTurn, Behavior = snapshot,
            Rejected = rejected?.ToImmutableArray() ?? [], Planning = planning, StateKey = StateKey(resolver),
            Ties = selected is null ? [] : ordered.Where(entry => entry.ObjectiveRank == selected.ObjectiveRank && entry.Utility == selected.Utility)
                .Select(entry => entry.Candidate.Key).ToImmutableArray(),
        };
        resolver.LastDecision = trace;
        return trace;
    }

    /// <summary>Decides and commits synchronously without repeating the stale-state check.</summary>
    public static DecisionTrace DecideAndCommit(CombatActionResolver resolver, string actorId)
    {
        var trace = Decide(resolver, actorId);
        if (trace.Selected is { } selection) resolver.Commit(trace.ActorId, selection);
        else resolver.Combat.SubmitNoLegalAction();
        return trace;
    }
    public static DecisionTrace CommitDecision(CombatActionResolver resolver, DecisionTrace trace)
    {
        if (trace.StateKey != StateKey(resolver) || trace.Selected is { } selection && !resolver.CanCommit(trace.ActorId, selection))
            trace = Decide(resolver, trace.ActorId);
        if (trace.Selected is { } current) resolver.Commit(trace.ActorId, current); else resolver.Combat.SubmitNoLegalAction();
        return trace;
    }
    public static string StateKey(CombatActionResolver resolver)
    {
        var state = ActionPreview.Capture(new(new ActionData { Damage = new DamageProfile() }, "", [], []), resolver);
        return System.Text.Json.JsonSerializer.Serialize(new { resolver.Combat.Timeline.CurrentTurn, resolver.Combat.Outcome,
            state.Characters, state.Schedule, Memory = resolver.Behavior.Save() });
    }

    private static StrategyId Effective(CombatActionResolver resolver, string actorId, BehaviorSnapshot snapshot)
    {
        var assignment = snapshot.Effective;
        if (assignment.Strategy == StrategyId.Protective && !Wards(resolver, actorId, assignment).Any(id => resolver.Characters[id].Health.Health > 0)) return assignment.Fallback;
        if (assignment.Strategy == StrategyId.Chaotic) return assignment.Modes[snapshot.Mode % assignment.Modes.Length];
        if (assignment.Strategy == StrategyId.Reactive)
        {
            var actor = resolver.Characters[actorId];
            var observed = snapshot.History.Where(entry => resolver.Characters[entry.SourceId].Team != actor.Team).ToArray();
            // Stable rule order: repeated restoration, repeated ranged actions, then repeated damaging actions.
            if (observed.Count(entry => entry.Effects.Any(effect => effect.Verb == EffectVerb.Restore && effect.ActualChange > 0)) >= 2) return StrategyId.Disruptive;
            if (observed.Count(entry => entry.Classifications.Contains("ranged")) >= 2) return StrategyId.Defensive;
            if (observed.Count(entry => entry.Effects.Any(effect => effect.Verb == EffectVerb.Damage && effect.ActualChange > 0)) >= 2) return StrategyId.Defensive;
            return assignment.Fallback;
        }
        return assignment.Strategy;
    }
    private static HashSet<string> Wards(CombatActionResolver resolver, string actorId, StrategyAssignment assignment) => assignment.ProtectAllies
        ? resolver.Characters.Values.Where(character => character.Id != actorId && character.Team == resolver.Characters[actorId].Team).Select(character => character.Id).ToHashSet(StringComparer.Ordinal)
        : (assignment.ProtectedIds ?? []).ToHashSet(StringComparer.Ordinal);

    private static ScoredCandidate Evaluate(CombatActionResolver before, CombatActionResolver after, string actorId,
        ActionCandidate candidate, ActionOutcome outcome, BehaviorSnapshot snapshot, StrategyId strategy,
        ThreatFacts? beforeThreat = null, ThreatFacts? afterThreat = null, bool captureTrace = true)
    {
        var weights = snapshot.Effective.Weights ?? StrategyWeights.For(strategy);
        var actor = before.Characters[actorId];
        var wards = Wards(before, actorId, snapshot.Effective);
        var contributions = ImmutableArray.CreateBuilder<ScoreContribution>();
        long utility = 0;
        void Add(string feature, long value, int weight)
        {
            if (value == 0) return;
            var weighted = checked(value * weight);
            utility = checked(utility + (feature == "time-centiturns" ? weighted / 100 : weighted));
            if (captureTrace) contributions.Add(new(feature, value, weight, weighted));
        }
        foreach (var (id, character) in before.Characters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var next = after.Characters[id];
            var loss = (long)character.Health.Health - next.Health.Health;
            if (character.Team != actor.Team)
            {
                Add($"damage:{id}", loss, weights.Damage);
                if (character.Health.Health > 0 && next.Health.Health == 0) Add($"elimination:{id}", 1, weights.Elimination);
                var observedOpening = snapshot.History.Any(entry => entry.Effects.Any(effect => effect.RecipientId == id
                    && effect.Verb == EffectVerb.Damage && effect.ActualChange > 0 && effect.ActualChange >= effect.RequestedAmount
                    && candidate.Action.BuildEffects().OfType<DamageEffect>().Any(damage => damage.Type == effect.DamageType)));
                if (loss > 0 && observedOpening) Add($"observed-opening:{id}", loss, weights.Opening);
            }
            else
            {
                var weight = id == actorId ? weights.SelfPreservation : wards.Contains(id) ? weights.WardPreservation : weights.AllyPreservation;
                if (loss < 0 && beforeThreat is not null)
                {
                    // No fictitious maximum: surplus restoration is real, with one tenth
                    // marginal value above a reserve of two observed immediate threats.
                    var useful = Math.Min(-loss, Math.Max(0L, 2L * beforeThreat.Harm.GetValueOrDefault(id) - character.Health.Health));
                    Add($"survival-restoration:{id}", useful, weight);
                    Add($"surplus-restoration:{id}", -loss - useful, weight / 10);
                }
                else Add($"health:{id}", -loss, weight);
                if (beforeThreat is not null && afterThreat is not null)
                {
                    var oldHarm = Math.Min(character.Health.Health, beforeThreat.Harm.GetValueOrDefault(id));
                    var newHarm = Math.Min(next.Health.Health, afterThreat.Harm.GetValueOrDefault(id));
                    Add($"future-harm-prevented:{id}", oldHarm - newHarm, weight);
                    if (character.Health.Health > 0 && character.Health.Health <= oldHarm && next.Health.Health > newHarm)
                        Add($"preservation:{id}", 5, weight);
                }
                Add($"focus:{id}", (long)next.Loadout.Focus - character.Loadout.Focus,
                    next.Loadout.Focus < character.Loadout.Focus ? weights.Focus : weights.Support);
                var itemIds = character.Loadout.Items.Select(item => item.Id.Value).Union(next.Loadout.Items.Select(item => item.Id.Value)).Order(StringComparer.Ordinal);
                foreach (var item in itemIds)
                    Add($"item:{id}:{item}", (long)(next.Loadout.Items.Find(owned => owned.Id.Value == item)?.Quantity ?? 0)
                        - (character.Loadout.Items.Find(owned => owned.Id.Value == item)?.Quantity ?? 0), weights.Item);
            }
        }
        if (beforeThreat is not null && afterThreat is not null)
        {
            // An action is an opportunity, regardless of its number of equivalent targeting anchors.
            bool LivingOpponent(string key) => after.Characters.Values.Any(character => character.Team != actor.Team && character.Health.Health > 0 && key.StartsWith(character.Id + "/", StringComparison.Ordinal));
            Add("opposing-actions-denied", beforeThreat.Actions.Except(afterThreat.Actions).Count(LivingOpponent), weights.Denial);
            Add("opposing-actions-enabled", -afterThreat.Actions.Except(beforeThreat.Actions).Count(LivingOpponent), weights.Denial);
        }
        Add("schedule-opportunities", ScheduleAdvantage(before, after, actorId, candidate.Action.TurnCost), weights.Denial);
        Add("allied-actions-enabled", beforeThreat is not null && afterThreat is not null
            ? afterThreat.AlliedActions.Except(beforeThreat.AlliedActions).Count()
            : EnabledActions(after, actor.Team).Except(EnabledActions(before, actor.Team)).Count(), weights.Support);
        Add("time-centiturns", -(long)Math.Round(candidate.Action.TurnCost * 100d, MidpointRounding.AwayFromZero), weights.Time);
        // Time is expressed in centiturns; the declared weight is per Turn, avoiding float tie comparisons.
        return new(candidate, captureTrace ? ActionPreview.Capture(outcome, after) : null, utility,
            ObjectiveRank(after.Combat.Outcome, actor.Team), captureTrace ? contributions.ToImmutable() : []);
    }
    private static int ObjectiveRank(CombatOutcome? outcome, CombatTeam team) => outcome switch
    {
        CombatOutcome.Victory => team == CombatTeam.Enemies ? 0 : 2,
        CombatOutcome.Defeat => team == CombatTeam.Enemies ? 2 : 0,
        _ => 1,
    };
    private static int ScheduleAdvantage(CombatActionResolver before, CombatActionResolver after, string actorId, float cost)
    {
        var oldEntries = before.Combat.Timeline.TurnOrder.Where(entry => entry.CombatantId is not null).ToList();
        if (before.Combat.ActiveCombatant is { } active)
            oldEntries.Add(new(-1, active.Turn + cost, TimelinePriority.Combatant, long.MaxValue, actorId, actorId,
                Initiative: active.Initiative, Team: active.Team, Row: active.Row, RandomRank: active.RandomRank));
        var oldTimes = CombatTimeline.OrderOccurrences(oldEntries).Select((entry, index) => (entry, index)).ToDictionary(pair => pair.entry.CombatantId!, pair => pair.index);
        var newTimes = after.Combat.Timeline.TurnOrder.Where(entry => entry.CombatantId is not null).Select((entry, index) => (entry, index)).ToDictionary(pair => pair.entry.CombatantId!, pair => pair.index);
        var team = before.Characters[actorId].Team;
        var result = 0;
        foreach (var opponent in before.Characters.Values.Where(character => character.Team != team))
            foreach (var ally in before.Characters.Values.Where(character => character.Team == team))
                if (oldTimes.TryGetValue(opponent.Id, out var oldOpponent) && oldTimes.TryGetValue(ally.Id, out var oldAlly)
                    && newTimes.TryGetValue(opponent.Id, out var newOpponent) && newTimes.TryGetValue(ally.Id, out var newAlly))
                    result += (oldOpponent < oldAlly && newOpponent > newAlly ? 1 : 0) - (oldOpponent > oldAlly && newOpponent < newAlly ? 1 : 0);
        return result;
    }
    private static ThreatFacts Threats(CombatActionResolver resolver, string actorId, Dictionary<string, ThreatFacts>? cache = null)
    {
        // Equivalent inventory-cell permutations share local threat facts. This cache is
        // decision-local and keyed by every resource/rule/ownership input used by probes.
        var key = System.Text.Json.JsonSerializer.Serialize(new
        {
            resolver.Combat.HasEnded,
            Characters = resolver.Characters.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new
            {
                pair.Key,
                Health = pair.Value.Health.Health,
                Focus = pair.Value.Loadout.Focus,
                pair.Value.Position,
                Items = pair.Value.Loadout.Items.OrderBy(item => item.Id.Value, StringComparer.Ordinal)
                    .Select(item => new { Id = item.Id.Value, item.Quantity }),
                Actions = pair.Value.Loadout.Actions.Keys.Order(StringComparer.Ordinal),
                Statuses = pair.Value.Statuses.OrderBy(status => status.Sequence)
                    .Select(status => FormattableString.Invariant($"{status.Definition.Id}:{status.Sequence}:{status.RemainingUses}:{status.ExpiresAt}:{status.ExpiresOnTurnStart}")),
                Equipment = pair.Value.Loadout.Equipment.OrderBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => new { item.Key, item.Value }),
                Shots = pair.Value.Loadout.Items.Where(item => item.Weapon?.Shots > 0)
                    .OrderBy(item => item.Id.Value, StringComparer.Ordinal)
                    .Select(item => new { Id = item.Id.Value, Remaining = pair.Value.Loadout.ShotsRemaining(item) }),
                pair.Value.TurnsStarted,
            }),
        });
        if (cache?.TryGetValue(key, out var cached) == true) return cached;
        var harm = new Dictionary<string, int>(StringComparer.Ordinal);
        var actions = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        if (!resolver.Combat.HasEnded)
            foreach (var opponent in resolver.Characters.Values.Where(character => character.Team != resolver.Characters[actorId].Team
                && resolver.Combat.IsAlive(character.Id)).OrderBy(character => character.Id, StringComparer.Ordinal))
            {
                var probe = resolver.CopyForSimulation();
                probe.Combat.OpenThreatProbe(opponent.Id);
                foreach (var candidate in CandidateGeneration.Enumerate(probe, opponent.Id, includeLayouts: false))
                {
                    if (candidate.Action.Id == "wait") continue;
                    var simulation = probe.CopyForSimulation();
                    var actionOutcome = simulation.Commit(opponent.Id, candidate);
                    var useful = false;
                    foreach (var ally in resolver.Characters.Values.Where(character => character.Team == resolver.Characters[actorId].Team))
                    {
                        var damage = Math.Max(0, ally.Health.Health - simulation.Characters[ally.Id].Health.Health);
                        harm[ally.Id] = Math.Max(harm.GetValueOrDefault(ally.Id), damage);
                        if (damage > 0) useful = true;
                    }
                    if (useful || actionOutcome.Effects.Any(effect => effect.ActualChange != 0)) actions.Add(opponent.Id + "/" + candidate.Key);
                }
            }
        var result = new ThreatFacts(harm.ToImmutableDictionary(StringComparer.Ordinal), actions.ToImmutable(), EnabledActions(resolver, resolver.Characters[actorId].Team).ToImmutableHashSet(StringComparer.Ordinal));
        cache?.Add(key, result);
        return result;
    }
    private static HashSet<string> EnabledActions(CombatActionResolver resolver, CombatTeam team)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (resolver.Combat.HasEnded) return result;
        foreach (var character in resolver.Characters.Values.Where(character => character.Team == team && resolver.Combat.IsAlive(character.Id)))
        {
            var probe = resolver.CopyForSimulation();
            probe.Combat.OpenThreatProbe(character.Id);
            foreach (var candidate in CandidateGeneration.Enumerate(probe, character.Id, includeLayouts: false))
                if (candidate.Action.Id is not ("wait" or "escape")) result.Add(character.Id + "/" + candidate.Key);
        }
        return result;
    }

    private static PlanningTrace Plan(CombatActionResolver resolver, string actorId, BehaviorSnapshot snapshot,
        IReadOnlyList<ActionCandidate> candidates, List<ScoredCandidate> evaluations)
    {
        var settings = snapshot.Effective;
        var nodes = 0;
        var truncated = false;
        foreach (var candidate in candidates)
        {
            if (nodes >= settings.NodeBudget) { truncated = true; break; }
            nodes++;
            var copy = resolver.CopyForSimulation();
            var outcome = copy.Commit(actorId, candidate);
            var complete = true;
            for (var depth = 1; depth < settings.DecisionDepth && !copy.Combat.HasEnded; depth++)
            {
                while (!copy.Combat.HasEnded && copy.Combat.ActiveCombatant is null)
                {
                    if (nodes >= settings.NodeBudget) { complete = false; truncated = true; break; }
                    nodes++;
                    copy.Combat.Process(1);
                    if (!copy.Combat.HasEnded && copy.Combat.ActiveCombatant is null && copy.Combat.Timeline.Preview(1).Count == 0)
                        throw new InvalidOperationException("Planning encounter has no pending occurrence or terminal outcome.");
                }
                if (!complete) break;
                if (copy.Combat.HasEnded) break;
                if (copy.Combat.ActiveCombatant is not { } actor)
                    throw new InvalidOperationException("Planning encounter has no pending decision or terminal outcome.");
                var continuation = actor.Team == resolver.Characters[actorId].Team ? settings.Continuation : settings.OpposingContinuation;
                var continuationSnapshot = copy.Behavior.Snapshot(actor.Id, new(continuation));
                ScoredCandidate? best = null;
                foreach (var choice in CandidateGeneration.Enumerate(copy, actor.Id))
                {
                    if (nodes >= settings.NodeBudget) { complete = false; truncated = true; break; }
                    nodes++;
                    var next = copy.CopyForSimulation();
                    var nextOutcome = next.Commit(actor.Id, choice);
                    var evaluation = Evaluate(copy, next, actor.Id, choice, nextOutcome, continuationSnapshot, continuation,
                        captureTrace: false);
                    if (best is null || evaluation.ObjectiveRank > best.ObjectiveRank || evaluation.ObjectiveRank == best.ObjectiveRank && evaluation.Utility > best.Utility)
                        best = evaluation;
                }
                if (!complete) break;
                if (best is null) copy.Combat.SubmitNoLegalAction(); else copy.Commit(actor.Id, best.Candidate);
            }
            if (complete) evaluations.Add(Evaluate(resolver, copy, actorId, candidate, outcome, snapshot, settings.Fallback,
                captureTrace: resolver.CaptureCandidateTraces));
            if (truncated) break;
        }
        var fallback = evaluations.Count == 0 && candidates.Count > 0;
        if (fallback)
        {
            var threatCache = new Dictionary<string, ThreatFacts>(StringComparer.Ordinal);
            var threats = Threats(resolver, actorId, threatCache);
            foreach (var candidate in candidates)
            {
                var copy = resolver.CopyForSimulation();
                var outcome = copy.Commit(actorId, candidate);
                evaluations.Add(Evaluate(resolver, copy, actorId, candidate, outcome, snapshot, settings.Fallback,
                    threats, Threats(copy, actorId, threatCache), resolver.CaptureCandidateTraces));
            }
        }
        return new(settings.DecisionDepth, settings.NodeBudget, nodes, truncated, fallback, settings.Continuation, settings.OpposingContinuation);
    }
}
