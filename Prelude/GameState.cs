namespace NexusRealms.Prelude;

public sealed class GameState
{
    public GameState(StoryNodeId currentStoryNodeId) => CurrentStoryNodeId = currentStoryNodeId;

    public StoryNodeId CurrentStoryNodeId { get; set; }
    public CombatOutcome? LastCombatOutcome { get; set; }
    /// <summary>The selected zodiac's stable identity, used to resolve its progression graph.</summary>
    public ZodiacSignId Zodiac { get; init; } = ZodiacSignId.Aquarius;
    public HashSet<StoryNodeId> CompletedProgressionNodes { get; } = [];

    /// <summary>Gets the player's portrait texture.</summary>
    public ContentId Portrait { get; init; } = new("portraits.mercenary_male.png");
    public Combat.CombatLoadout Loadout { get; init; } = Combat.CombatLoadout.CreatePlayerLoadout();

    /// <summary>Active status effect identifiers shared by the HUD and Character dialog.</summary>
    public StatusEffectId[] StatusEffects { get; init; } = ["guarded", "inspired", "focused"];
    private double _gameSpeed = 1d;

    /// <summary>Combat playback multiplier; 2 means twice as fast.</summary>
    public double GameSpeed
    {
        get => _gameSpeed;
        set
        {
            if (!double.IsFinite(value) || value < 0.1d || value > 10d)
                throw new ArgumentOutOfRangeException(nameof(value));
            _gameSpeed = value;
        }
    }
    private int _abilityPoints = 20;

    /// <summary>Unspent progression points; learning an ability costs one point.</summary>
    public int AbilityPoints
    {
        get => _abilityPoints;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _abilityPoints = value;
        }
    }
    public int Health { get; set; } = 20;
    public int Focus
    {
        get => Loadout.Focus;
        init => Loadout.Focus = value;
    }
}
