namespace NexusRealms.Prelude.DataModel;

/// <summary>
/// Defines an immutable character available to story nodes.
/// </summary>
public sealed record CharacterData
{
    /// <summary>
    /// Gets the character's unique identifier.
    /// </summary>
    public required CharacterId Id { get; init; }

    /// <summary>
    /// Gets the character's display name.
    /// </summary>
    public required string Name { get; init; }
    public StatusEffectId[] StatusEffects { get; init; } = [];
    public int Health { get; init; } = 5;
    public Combat.CarriedItem[] Items { get; init; } = [];
    public EnemyAttackData[] Attacks { get; init; } = [new()];
    public int Focus { get; init; } = 2;

    /// <summary>
    /// Gets the identifier of the character's artwork.
    /// </summary>
    public required ContentId Artwork { get; init; }

    /// <summary>Gets the character's turn-order portrait texture.</summary>
    public required ContentId Portrait { get; init; }
}


public sealed record EnemyAttackData
{
    public ItemId? ItemId { get; init; }
    public bool Throw { get; init; }
    public bool Reload { get; init; }
    public string Animation { get; init; } = "Attack";
    public string ReloadAnimation { get; init; } = "Idle";
    public float TurnCost { get; init; } = 1f;
    public Combat.DamageProfile Damage { get; init; } = new(new Dictionary<DamageType, int> { [DamageType.General] = 1 });
    public float ImpactFraction { get; init; } = 0.6f;
}