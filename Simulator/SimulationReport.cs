using System.Globalization;
using System.Text;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Simulator;

/// <summary>Builds report matrices from the dimensions captured on each simulation run.</summary>
public static class SimulationReport
{
    private static readonly CombatOutcome[] OutcomeOrder =
        [CombatOutcome.Victory, CombatOutcome.Defeat, CombatOutcome.Draw, CombatOutcome.Escape];

    /// <summary>Formats strategy, decisive-action, and action-usage projections for completed runs.</summary>
    /// <param name="results">The completed simulation runs.</param>
    /// <returns>A tab-separated report with outcome counts, rates, averages, and use distributions.</returns>
    /// <exception cref="ArgumentNullException">The results collection is null.</exception>
    /// <exception cref="ArgumentException">The results collection is empty.</exception>
    public static string Format(IReadOnlyList<SimulationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0) throw new ArgumentException("At least one simulation result is required.", nameof(results));

        var report = new StringBuilder();
        AppendStrategyOutcomes(report, results);
        AppendDecidingActionOutcomes(report, results);
        AppendActionUsage(report, results);
        AppendActionUseDistribution(report, results);
        AppendStrategyDecidingActions(report, results);
        return report.ToString();
    }

    /// <summary>Formats one traced AI decision, its alternatives, rationale, and predicted resolution.</summary>
    /// <param name="decision">The decision and candidate evaluations captured by the strategy.</param>
    /// <param name="characterNames">Display names keyed by runtime character identifier.</param>
    /// <param name="itemNames">Display names keyed by item identifier.</param>
    /// <returns>A readable turn-by-turn decision block.</returns>
    public static string FormatDecisionTrace(DecisionTrace decision, IReadOnlyDictionary<string, string> characterNames,
        IReadOnlyDictionary<string, string> itemNames)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(characterNames);
        ArgumentNullException.ThrowIfNull(itemNames);

        string CharacterName(string id) => characterNames.GetValueOrDefault(id, id);
        string ItemName(string id) => itemNames.GetValueOrDefault(id, id);
        bool IsEquipmentLayout(CandidateEvaluation evaluation) =>
            evaluation.Candidate.Kind == CandidateKind.ItemLayout && evaluation.Candidate.Action.Id == "equipment";
        string CandidateName(CandidateEvaluation evaluation)
        {
            var candidate = evaluation.Candidate;
            string name;
            if (candidate.Action.Reload)
            {
                var reloads = string.Join(", ", candidate.Reloads.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{ItemName(pair.Key)} with {ItemName(pair.Value)}"));
                name = $"Reload {reloads}";
            }
            else if (IsEquipmentLayout(evaluation))
            {
                var layout = string.Join(", ", candidate.Layout.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Where(pair => !string.IsNullOrEmpty(pair.Value))
                    .Select(pair => $"{pair.Key}={ItemName(pair.Value)}"));
                name = $"Equipment layout [{layout}]";
            }
            else
            {
                name = candidate.Action.Id;
                var itemId = candidate.ItemId ?? candidate.Action.ItemId?.Value;
                if (itemId is not null) name += $" ({ItemName(itemId)})";
            }

            var recipients = evaluation.Preview.Outcome.Recipients;
            if (recipients.Length > 0) name += $" -> {string.Join(", ", recipients.Select(CharacterName))}";
            else if (candidate.TargetId is { } targetId) name += $" -> {CharacterName(targetId)}";
            return name;
        }

        var report = new StringBuilder();
        report.Append("Turn ").Append(decision.Turn.ToString("F2", CultureInfo.InvariantCulture))
            .Append(" | ").Append(CharacterName(decision.ActorId))
            .Append(" | ").Append(decision.Strategy);
        if (decision.Behavior is { } behavior)
            report.Append(" | mode ").Append(behavior.Mode)
                .Append(" | history ").Append(behavior.History.Length);
        report.AppendLine();

        if (decision.Behavior is { History.Length: > 0 } snapshot)
            foreach (var observed in snapshot.History)
                report.Append("  Observed: ").Append(CharacterName(observed.SourceId)).Append(" used ")
                    .Append(observed.ActionId).Append(" [").Append(string.Join(", ", observed.Classifications)).AppendLine("]");

        if (decision.Planning is { } planning)
            report.Append("  Planning: depth=").Append(planning.Depth).Append(", budget=").Append(planning.Budget)
                .Append(", explored=").Append(planning.ExploredNodes).Append(", truncated=").Append(planning.Truncated)
                .Append(", fallback=").Append(planning.UsedFallback).AppendLine();

        if (decision.Selected is not { } selected)
        {
            report.AppendLine("  No legal action was selected.");
            return report.ToString();
        }

        var ordered = decision.Candidates.OrderByDescending(entry => entry.ObjectiveRank)
            .ThenByDescending(entry => entry.Utility).ToArray();
        var chosen = ordered.FirstOrDefault(entry => entry.Candidate.Key == selected.Key);
        if (chosen is not null)
        {
            report.Append("  Chosen: ").Append(CandidateName(chosen))
                .Append(" | objective=").Append(chosen.ObjectiveRank)
                .Append(" | utility=").Append(chosen.Utility).AppendLine();
            foreach (var contribution in chosen.Contributions.OrderByDescending(entry => Math.Abs((double)entry.Utility)))
            {
                var utility = contribution.Feature == "time-centiturns" ? contribution.Utility / 100 : contribution.Utility;
                report.Append("    Why: ").Append(contribution.Feature).Append(" value=").Append(contribution.Value)
                    .Append(" weight=").Append(contribution.Weight).Append(" utility=").Append(utility).AppendLine();
            }
        }
        else report.Append("  Chosen: ").Append(selected.Key).AppendLine();

        var considered = ordered.Where(evaluation =>
            !IsEquipmentLayout(evaluation) || evaluation.Candidate.Key == selected.Key).ToArray();
        var omittedEquipmentLayouts = ordered.Length - considered.Length;
        report.Append("  Considered actions:");
        if (omittedEquipmentLayouts > 0)
            report.Append(' ').Append(omittedEquipmentLayouts).Append(" equipment layouts omitted");
        report.AppendLine();
        foreach (var evaluation in considered)
            report.Append("    ").Append(evaluation.Candidate.Key == selected.Key ? "> " : "  ")
                .Append(CandidateName(evaluation)).Append(" | objective=").Append(evaluation.ObjectiveRank)
                .Append(" | utility=").Append(evaluation.Utility).AppendLine();

        if (chosen is not null)
        {
            foreach (var effect in chosen.Preview.Outcome.Effects)
            {
                report.Append("  Effect: ").Append(effect.Verb).Append(" -> ").Append(CharacterName(effect.RecipientId))
                    .Append(" requested=").Append(effect.RequestedAmount.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append(" actual=").Append(effect.ActualChange.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append(" result=").Append(effect.Result);
                if (!string.IsNullOrWhiteSpace(effect.Reason)) report.Append(" reason=").Append(effect.Reason);
                report.AppendLine();
            }
            foreach (var recipientId in chosen.Preview.Outcome.Recipients.Distinct(StringComparer.Ordinal))
                if (chosen.Preview.Characters.TryGetValue(recipientId, out var recipient))
                    report.Append("  After: ").Append(CharacterName(recipientId)).Append(" health=").Append(recipient.Health)
                        .Append(" focus=").Append(recipient.Focus).AppendLine();
        }
        return report.ToString();
    }

    /// <summary>Appends run counts and victory rates grouped by player strategy.</summary>
    /// <param name="report">The report being assembled.</param>
    /// <param name="results">The completed simulation runs.</param>
    private static void AppendStrategyOutcomes(StringBuilder report, IReadOnlyList<SimulationResult> results)
    {
        report.AppendLine("Player strategy x outcome (run counts; victory rate):");
        report.Append("Strategy");
        AppendOutcomeColumns(report);
        report.AppendLine("\tVictory%");
        foreach (var group in results.GroupBy(result => result.PlayerStrategy).OrderBy(group => group.Key))
        {
            report.Append(group.Key);
            AppendOutcomeCounts(report, group);
            report.Append('\t').Append(Percent(Count(group, CombatOutcome.Victory), group.Count())).AppendLine();
        }
        report.AppendLine();
    }

    /// <summary>Appends terminal decisive actions grouped by encounter outcome.</summary>
    /// <param name="report">The report being assembled.</param>
    /// <param name="results">The completed simulation runs.</param>
    private static void AppendDecidingActionOutcomes(StringBuilder report, IReadOnlyList<SimulationResult> results)
    {
        report.AppendLine("Deciding action x outcome (the resolution that reached a terminal condition):");
        report.Append("Action\tSource\tTerminal condition");
        AppendOutcomeColumns(report);
        report.AppendLine();
        foreach (var group in results.GroupBy(result => (
                     Action: result.DecidingAction ?? "(none)",
                     Source: result.DecidingActionSource ?? "(none)",
                     Condition: result.TerminalCondition))
                 .OrderBy(group => group.Key.Action, StringComparer.Ordinal)
                 .ThenBy(group => group.Key.Source, StringComparer.Ordinal)
                 .ThenBy(group => group.Key.Condition, StringComparer.Ordinal))
        {
            report.Append(group.Key.Action).Append('\t').Append(group.Key.Source).Append('\t').Append(group.Key.Condition);
            AppendOutcomeCounts(report, group);
            report.AppendLine();
        }
        report.AppendLine();
    }

    /// <summary>Appends total and per-run action use counts for each outcome.</summary>
    /// <param name="report">The report being assembled.</param>
    /// <param name="results">The completed simulation runs.</param>
    private static void AppendActionUsage(StringBuilder report, IReadOnlyList<SimulationResult> results)
    {
        report.AppendLine("Player action usage x outcome (averages include runs with zero uses):");
        report.AppendLine("Outcome\tAction\tTotal uses\tRuns used\tAverage uses per run");
        foreach (var outcome in OutcomeOrder)
        {
            var runs = results.Where(result => result.Outcome == outcome).ToArray();
            if (runs.Length == 0) continue;
            var actions = runs.SelectMany(result => result.PlayerActionUses.Keys)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            foreach (var action in actions)
            {
                var total = runs.Sum(result => Uses(result, action));
                var used = runs.Count(result => Uses(result, action) > 0);
                report.Append(outcome).Append('\t').Append(action).Append('\t').Append(total).Append('\t').Append(used)
                    .Append('\t').Append((total / (double)runs.Length).ToString("F2", CultureInfo.InvariantCulture)).AppendLine();
            }
        }
        report.AppendLine();
    }

    /// <summary>Appends per-run action-use frequency buckets for each outcome.</summary>
    /// <param name="report">The report being assembled.</param>
    /// <param name="results">The completed simulation runs.</param>
    private static void AppendActionUseDistribution(StringBuilder report, IReadOnlyList<SimulationResult> results)
    {
        report.AppendLine("Player action use distribution (runs per use count):");
        report.AppendLine("Action\tOutcome\t0\t1\t2\t3\t4+");
        var actions = results.SelectMany(result => result.PlayerActionUses.Keys)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        foreach (var action in actions)
        foreach (var outcome in OutcomeOrder)
        {
            var runs = results.Where(result => result.Outcome == outcome).ToArray();
            if (runs.Length == 0) continue;
            var buckets = new int[5];
            foreach (var run in runs)
            {
                var count = Uses(run, action);
                buckets[Math.Min(count, 4)]++;
            }
            report.Append(action).Append('\t').Append(outcome);
            foreach (var bucket in buckets) report.Append('\t').Append(bucket);
            report.AppendLine();
        }
        report.AppendLine();
    }

    /// <summary>Appends the strategy, enemy-configuration, and decisive-action cross-tabulation.</summary>
    /// <param name="report">The report being assembled.</param>
    /// <param name="results">The completed simulation runs.</param>
    private static void AppendStrategyDecidingActions(StringBuilder report, IReadOnlyList<SimulationResult> results)
    {
        report.AppendLine("Player strategy x enemy configuration x deciding action:");
        report.AppendLine("Strategy\tEnemy configuration\tAction\tSource\tOutcome\tRuns");
        foreach (var group in results.GroupBy(result => (
                     Strategy: result.PlayerStrategy,
                     Enemy: EnemyConfigurationKey(result.EnemyConfiguration),
                     Action: result.DecidingAction ?? "(none)",
                     Source: result.DecidingActionSource ?? "(none)",
                     result.Outcome))
                 .OrderBy(group => group.Key.Strategy)
                 .ThenBy(group => group.Key.Enemy, StringComparer.Ordinal)
                 .ThenBy(group => group.Key.Action, StringComparer.Ordinal)
                 .ThenBy(group => group.Key.Source, StringComparer.Ordinal)
                 .ThenBy(group => group.Key.Outcome))
        {
            report.Append(group.Key.Strategy).Append('\t').Append(group.Key.Enemy).Append('\t')
                .Append(group.Key.Action).Append('\t').Append(group.Key.Source).Append('\t')
                .Append(group.Key.Outcome).Append('\t').Append(group.Count()).AppendLine();
        }
    }

    /// <summary>Appends outcome headings to a matrix row.</summary>
    /// <param name="report">The report being assembled.</param>
    private static void AppendOutcomeColumns(StringBuilder report)
    {
        foreach (var outcome in OutcomeOrder) report.Append('\t').Append(outcome);
    }

    /// <summary>Appends counts for each supported encounter outcome.</summary>
    /// <param name="report">The report being assembled.</param>
    /// <param name="results">The simulation runs represented by the row.</param>
    private static void AppendOutcomeCounts(StringBuilder report, IEnumerable<SimulationResult> results)
    {
        var runs = results.ToArray();
        foreach (var outcome in OutcomeOrder) report.Append('\t').Append(Count(runs, outcome));
    }

    /// <summary>Counts runs with the requested encounter outcome.</summary>
    /// <param name="results">The simulation runs to count.</param>
    /// <param name="outcome">The encounter outcome to count.</param>
    /// <returns>The number of matching runs.</returns>
    private static int Count(IEnumerable<SimulationResult> results, CombatOutcome outcome) =>
        results.Count(result => result.Outcome == outcome);

    /// <summary>Returns one action's use count for a run.</summary>
    /// <param name="result">The completed simulation run.</param>
    /// <param name="action">The action label.</param>
    /// <returns>The number of uses, or zero when the action was unused.</returns>
    private static int Uses(SimulationResult result, string action) =>
        result.PlayerActionUses.GetValueOrDefault(action);

    /// <summary>Formats a percentage using invariant decimal notation.</summary>
    /// <param name="count">The numerator.</param>
    /// <param name="total">The denominator.</param>
    /// <returns>The percentage to two decimal places.</returns>
    private static string Percent(int count, int total) =>
        (100d * count / total).ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>Creates a deterministic human-readable key for an enemy configuration.</summary>
    /// <param name="enemies">The enemy and ally placements in one scenario.</param>
    /// <returns>A stable key describing character strategies and placements.</returns>
    private static string EnemyConfigurationKey(IReadOnlyList<SimulationEnemyConfiguration> enemies) =>
        enemies.Count == 0
            ? "(none)"
            : string.Join("; ", enemies.OrderBy(enemy => enemy.CharacterId, StringComparer.Ordinal)
                .ThenBy(enemy => enemy.Team).ThenBy(enemy => enemy.Slot)
                .Select(enemy => string.Create(CultureInfo.InvariantCulture,
                    $"{enemy.CharacterId}[{enemy.Strategy},{enemy.Team}@{enemy.Slot}:t={enemy.InitialTurn:F2},i={enemy.Initiative}]")));
}
