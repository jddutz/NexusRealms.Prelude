namespace NexusRealms.Prelude.DataModel;

public sealed record ActionData
{
    public ItemId? ItemId { get; init; }
    public bool Throw { get; init; }
    public bool Reload { get; init; }
    public string Animation { get; init; } = "Attack";
    public string ReloadAnimation { get; init; } = "Idle";
    public float TurnCost { get; init; } = 1f;
    public Combat.DamageProfile Damage { get; init; } =
        new(new Dictionary<DamageType, int> { [DamageType.General] = 1 });
    public float ImpactFraction { get; init; } = 0.6f;
}
