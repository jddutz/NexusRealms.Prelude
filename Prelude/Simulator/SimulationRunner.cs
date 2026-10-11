using NexusRealms.Prelude;
using NexusRealms.Prelude.Combat;
using NexusRealms.Prelude.Database;
using NexusRealms.Prelude.DataModel;

namespace NexusRealms.Prelude.Simulator;

public sealed record SimulationResult(int Seed, CombatOutcome Outcome, float Turn, int PlayerHealth, int Actions);

public static class SimulationRunner
{
    public const float TurnLimit = 100f;

    public static SimulationResult Run(Storyline storyline, SimulationState initial, int seed)
    {
        var state = initial.CreateGameState();
        if (!storyline.Nodes.TryGetValue(state.CurrentStoryNodeId, out var node) || node is not CombatScenario scenario)
            throw new ArgumentException($"'{state.CurrentStoryNodeId}' is not an authored combat scenario.");
        return Run(scenario, storyline.Characters, state, seed, initial.AllowEscape);
    }

    public static SimulationResult Run(CombatScenario scenario, IReadOnlyDictionary<CharacterId, CharacterData> characters,
        GameState state, int seed, bool allowEscape = false)
    {
        var random = new Random(seed);
        var combat = new CombatSystem(unchecked((uint)seed)) { TurnLimit = TurnLimit };
        var health = new Dictionary<string, DamageReceiver> { ["player"] = new(state.Health) };
        var brains = new Dictionary<string, EnemyActionState>();
        var placements = new Dictionary<string, CharacterPlacement>();
        combat.Add(new("player", true, initiative: scenario.PlayerInitiative));
        for (var index = 0; index < scenario.Characters.Length; index++)
        {
            var placement = scenario.Characters[index];
            var id = $"encounter-{index}";
            var definition = characters[placement.CharacterId];
            health[id] = new(definition.Health);
            placements[id] = placement;
            brains[id] = new(definition);
            combat.Add(new(id, false, placement.InitialTurn, placement.Initiative, placement.Team, Row(placement.Slot)));
        }
        combat.IsCombatantAlive = id => health.TryGetValue(id, out var receiver) && receiver.Health > 0;
        foreach (var (id, receiver) in health)
        {
            if (id == "player") receiver.HealthChanged += () => state.Health = receiver.Health;
            receiver.DamageTaken += result => { if (result.IsLethal) combat.Remove(id); };
            if (receiver.Health == 0) combat.Remove(id);
        }
        combat.VictoryConditions = scenario.BuildVictoryConditions();
        combat.DefeatConditions = scenario.DefeatConditions.ToArray();
        foreach (var occurrence in scenario.Events)
            combat.ScheduleEvent(occurrence.Turn, occurrence.Label, occurrence.Resolve, occurrence.Priority,
                occurrence.Icon, occurrence.IsVisible, occurrence.PresentationKey);

        CommandTarget Target(string id)
        {
            var occupied = placements.Where(pair => pair.Value.Team == CombatTeam.Enemies && combat.IsAlive(pair.Key))
                .Select(pair => Row(pair.Value.Slot)).ToHashSet();
            var placement = placements[id];
            return new(combat.Contains(id), combat.IsAlive(id), placement.Team, Row(placement.Slot),
                occupied.Contains(CombatRow.Front), occupied.Contains(CombatRow.Middle));
        }

        var actions = 0;
        combat.Process();
        while (!combat.HasEnded)
        {
            // Fail explicitly on malformed same-time events instead of reporting a balance result.
            if (++actions > 1_000_000) throw new InvalidOperationException("Encounter exceeded the safety operation budget.");
            if (combat.ActiveCombatant is not { } actor)
            {
                if (combat.Timeline.Preview(1).Count == 0) throw new InvalidOperationException("Encounter has no pending occurrences or outcome.");
                combat.Process();
                continue;
            }
            if (!actor.PlayerControlled)
            {
                var brain = brains[actor.Id];
                var attack = brain.Select(random);
                combat.SubmitAutomaticAction(new(attack.TurnCost, (_, _) =>
                {
                    // Matches Prelude: allied placements currently consume turns without attacking.
                    if (actor.Team == CombatTeam.Enemies)
                    {
                        brain.Apply(attack);
                        DamageResolver.Apply(health["player"], attack.Damage);
                    }
                }));
                continue;
            }
            var loadout = state.Loadout;
            var choices = new List<Action>();
            foreach (var command in loadout.Abilities.Concat([CombatLoadout.LeftHandStrike, CombatLoadout.RightHandStrike, CombatLoadout.Wait]).Distinct())
            {
                if (!command.RequiresTarget)
                {
                    if (loadout.CanConfirm(combat, "player", command, false))
                        choices.Add(() => loadout.Confirm(combat, "player", command, () => null, () => { }));
                    continue;
                }
                foreach (var id in placements.Keys.Where(id => loadout.CanConfirm(combat, "player", command, loadout.ValidTarget(command, Target(id)))))
                    choices.Add(() => loadout.Confirm(combat, "player", command, () => Target(id), () => { },
                        strike => DamageResolver.Apply(health[id], strike.DamagePoints)));
            }
            foreach (var option in loadout.ThrowOptions())
                foreach (var id in placements.Keys.Where(id => loadout.CanConfirm(combat, "player", CombatLoadout.Throw, loadout.ValidTarget(CombatLoadout.Throw, Target(id)))))
                    choices.Add(() => { loadout.PrepareThrow(option.Item.Id.Value); loadout.ConfirmPreparedThrow(combat, "player", () => Target(id), damage => DamageResolver.Apply(health[id], damage)); });
            var layout = loadout.BeginItemLayout();
            foreach (var source in layout.Keys)
                foreach (var destination in CombatLoadout.EquipmentSlots.Concat(Enumerable.Range(0, CombatLoadout.InventoryCapacity).Select(CombatLoadout.InventorySlot)))
                {
                    // Inventory shuffling alone adds no tactical option.
                    if (CombatLoadout.IsInventorySlot(source) && CombatLoadout.IsInventorySlot(destination)) continue;
                    var pending = new Dictionary<string, string>(layout);
                    if (loadout.TryMoveItem(pending, source, destination) && loadout.ItemLayoutCost(pending) > 0)
                        choices.Add(() => loadout.CommitItemLayout(combat, "player", pending));
                }
            foreach (var weapon in loadout.Items.ToArray())
                foreach (var ammo in loadout.Items.Where(ammo => loadout.CanReload(layout, weapon.Id.Value, ammo.Id.Value)))
                    choices.Add(() => loadout.CommitItemLayout(combat, "player", layout, new Dictionary<string, string> { [weapon.Id.Value] = ammo.Id.Value }));
            if (allowEscape && !combat.IsEscaping) choices.Add(() => combat.RequestEscape("player"));
            if (choices.Count == 0) combat.SubmitAction(new(1f, (_, _) => { }));
            else choices[random.Next(choices.Count)]();
        }
        state.LastCombatOutcome = combat.Outcome;
        return new(seed, combat.Outcome ?? throw new InvalidOperationException("Encounter ended without an outcome."),
            combat.Outcome == CombatOutcome.Draw ? TurnLimit : combat.Timeline.CurrentTurn, state.Health, actions);
    }

    private static CombatRow Row(FormationSlot slot) => slot >= FormationSlot.FrontLeft ? CombatRow.Front
        : slot >= FormationSlot.MiddleLeft ? CombatRow.Middle : CombatRow.Back;
}
