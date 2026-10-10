namespace NexusRealms.Prelude.Combat;

public sealed record ThrowOption(CarriedItem Item, string[] Slots)
{
    public string Source => Slots.Length == 0 ? "Inventory" : string.Join(" / ", Slots);
}

/// <summary>A single-use picker session. Opening and cancelling never mutate combat state.</summary>
public sealed class ThrowSelection
{
    private readonly CombatLoadout _loadout;
    internal CombatSystem Combat { get; }
    internal string ActorId { get; }
    internal Func<CommandTarget?> Target { get; }
    public IReadOnlyList<ThrowOption> Options { get; }
    private bool _closed;
    internal ThrowSelection(CombatLoadout loadout, CombatSystem combat, string actorId,
        Func<CommandTarget?> target, IReadOnlyList<ThrowOption> options)
    {
        _loadout = loadout; Combat = combat; ActorId = actorId; Target = target; Options = options;
    }
    public void Cancel() => _closed = true;
    public bool Choose(ThrowOption option, Action<DamageProfile> damage)
    {
        ArgumentNullException.ThrowIfNull(damage);
        if (_closed || !Options.Any(offered => ReferenceEquals(offered, option))) return false;
        _closed = true;
        return _loadout.CommitThrow(this, option, damage);
    }
}
