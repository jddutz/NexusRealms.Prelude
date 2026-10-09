namespace NexusRealms.Prelude;

/// <summary>Identifies one of the ten positions in a combat formation.</summary>
public enum FormationSlot
{
    /// <summary>Back row, left position.</summary>
    BackLeft = 0,

    /// <summary>Back row, center position.</summary>
    BackCenter = 1,

    /// <summary>Back row, right position.</summary>
    BackRight = 2,

    /// <summary>Middle row, left position.</summary>
    MiddleLeft = 3,

    /// <summary>Middle row, center-left position.</summary>
    MiddleCenterLeft = 4,

    /// <summary>Middle row, center-right position.</summary>
    MiddleCenterRight = 5,

    /// <summary>Middle row, right position.</summary>
    MiddleRight = 6,

    /// <summary>Front row, left position.</summary>
    FrontLeft = 7,

    /// <summary>Front row, center position.</summary>
    FrontCenter = 8,

    /// <summary>Front row, right position.</summary>
    FrontRight = 9,
}

/// <summary>Positions combat characters in world space using a staggered 3-4-3 formation.</summary>
public sealed class CharacterFormation : GameObject2D
{
    private const float CharacterHeightFraction = 0.62f;
    private const float CharacterScale = 0.75f;
    private float _verticalOffset;
    private float _maximumCharacterHeight = float.PositiveInfinity;

    private readonly Slot[] _slots =
    [
        new(10, new(0.35f, 0.62f), 0.70f),
        new(10, new(0.50f, 0.62f), 0.74f),
        new(10, new(0.65f, 0.62f), 0.70f),
        new(20, new(0.25f, 0.71f), 0.85f),
        new(20, new(0.42f, 0.71f), 0.90f),
        new(20, new(0.58f, 0.71f), 0.90f),
        new(20, new(0.75f, 0.71f), 0.85f),
        new(30, new(0.30f, 0.78f), 0.96f),
        new(30, new(0.50f, 0.80f), 1.00f),
        new(30, new(0.70f, 0.78f), 0.96f),
    ];

    /// <summary>
    /// Initializes a formation sized to the world area in which its characters are placed.
    /// </summary>
    /// <param name="worldSize">The width and height of the world-space combat area.</param>
    public CharacterFormation(Vector2D<float> worldSize)
    {
        if (
            !float.IsFinite(worldSize.X)
            || !float.IsFinite(worldSize.Y)
            || worldSize.X <= 0f
            || worldSize.Y <= 0f
        )
            throw new ArgumentOutOfRangeException(nameof(worldSize));

        WorldSize = worldSize;
    }

    /// <summary>Gets the world-space area used to calculate slot positions.</summary>
    public Vector2D<float> WorldSize { get; private set; }

    /// <summary>
    /// Updates the world-space area and reapplies every occupied slot's position and scale.
    /// </summary>
    /// <param name="worldSize">The new width and height of the world-space combat area.</param>
    public void SetWorldSize(Vector2D<float> worldSize)
    {
        if (
            !float.IsFinite(worldSize.X)
            || !float.IsFinite(worldSize.Y)
            || worldSize.X <= 0f
            || worldSize.Y <= 0f
        )
            throw new ArgumentOutOfRangeException(nameof(worldSize));

        if (WorldSize == worldSize)
            return;

        WorldSize = worldSize;
        foreach (var slot in _slots)
            if (slot.Occupant is { } character)
                PositionCharacter(slot, character);
    }

    /// <summary>Keeps the front row and character tops within the HUD-free world area.</summary>
    public void SetVerticalLimits(float top, float bottom)
    {
        if (!float.IsFinite(top) || !float.IsFinite(bottom) || bottom <= top)
            return;
        var offset = Math.Min(0f, bottom - WorldSize.Y * 0.80f);
        var maximumHeight = bottom - top;
        if (_verticalOffset == offset && _maximumCharacterHeight == maximumHeight)
            return;
        _verticalOffset = offset;
        _maximumCharacterHeight = maximumHeight;
        foreach (var slot in _slots)
            if (slot.Occupant is { } character)
                PositionCharacter(slot, character);
    }

    /// <summary>Gets or assigns the character occupying a formation slot.</summary>
    /// <param name="slot">The slot to access.</param>
    public Character? this[FormationSlot slot]
    {
        get => GetSlot(slot).Occupant;
        set => SetSlot(slot, value);
    }

    /// <summary>Assigns a character to a slot, or clears the slot when the value is null.</summary>
    /// <param name="slot">The slot to update.</param>
    /// <param name="character">The character to place.</param>
    public void SetSlot(FormationSlot slot, Character? character)
    {
        var target = GetSlot(slot);
        if (ReferenceEquals(target.Occupant, character))
            return;
        if (character is not null && character.Parent is not null)
            throw new ArgumentException(
                "Detach the character from its current parent first.",
                nameof(character)
            );

        if (target.Occupant is { } previous)
            RemoveChild(previous);
        target.Occupant = character;
        if (character is not null)
        {
            AddChild(character);
            PositionCharacter(target, character);
        }
    }

    /// <summary>Gets the character assigned to a slot.</summary>
    /// <param name="slot">The slot to inspect.</param>
    /// <returns>The assigned character, or null.</returns>
    public Character? GetSlotCharacter(FormationSlot slot) => GetSlot(slot).Occupant;

    /// <summary>Positions and scales one character from its normalized slot definition.</summary>
    /// <param name="slot">The slot containing the character.</param>
    /// <param name="character">The character to position.</param>
    private void PositionCharacter(Slot slot, Character character)
    {
        character.Position = new(
            WorldSize.X * slot.Anchor.X,
            WorldSize.Y * slot.Anchor.Y + _verticalOffset
        );
        var targetHeight =
            Math.Min(WorldSize.Y * CharacterHeightFraction, _maximumCharacterHeight) * slot.Scale * CharacterScale;
        var scale = targetHeight / character.Texture.Height;
        character.Scale = new(scale, scale);
        character.Renderer.DrawOrder = slot.SortOrder;
        character.ShadowRenderer.DrawOrder = slot.SortOrder - 1;
    }

    /// <summary>Gets a validated slot by its public enum value.</summary>
    /// <param name="slot">The slot to retrieve.</param>
    /// <returns>The internal slot definition.</returns>
    private Slot GetSlot(FormationSlot slot)
    {
        var index = (int)slot;
        if ((uint)index >= (uint)_slots.Length)
            throw new ArgumentOutOfRangeException(nameof(slot));
        return _slots[index];
    }

    private sealed class Slot(int sortOrder, Vector2D<float> anchor, float scale)
    {
        /// <summary>Gets or sets the character assigned to this slot.</summary>
        public Character? Occupant { get; set; }

        /// <summary>Gets the relative draw order of this slot.</summary>
        public int SortOrder { get; } = sortOrder;

        /// <summary>Gets the normalized world-space anchor of this slot.</summary>
        public Vector2D<float> Anchor { get; } = anchor;

        /// <summary>Gets the visual scale multiplier of this slot.</summary>
        public float Scale { get; } = scale;
    }
}

