namespace NexusRealms.Prelude;

public enum FormationSlot
{
    BackLeft = 0,
    BackCenter = 1,
    BackRight = 2,
    MiddleLeft = 3,
    MiddleCenterLeft = 4,
    MiddleCenterRight = 5,
    MiddleRight = 6,
    FrontLeft = 7,
    FrontCenter = 8,
    FrontRight = 9,
}

/// <summary>Arranges ten character slots in three staggered rows: 3, 4, 3.</summary>
public class CombatFormation : Element
{
    private readonly CharacterSlot[] _slots = [];

    public CombatFormation()
    {
        _slots =
        [
            // Sort order is relative to the formation; center slots draw last in each row.
            new(this, 0, 0.30f, 0.58f, 0.70f),
            new(this, 2, 0.50f, 0.61f, 0.74f),
            new(this, 1, 0.70f, 0.58f, 0.70f),
            new(this, 3, 0.20f, 0.73f, 0.85f),
            new(this, 5, 0.40f, 0.76f, 0.90f),
            new(this, 6, 0.60f, 0.76f, 0.90f),
            new(this, 4, 0.80f, 0.73f, 0.85f),
            new(this, 7, 0.25f, 0.90f, 0.96f),
            new(this, 9, 0.50f, 0.93f, 1.00f),
            new(this, 8, 0.75f, 0.90f, 0.96f),
        ];

        foreach (var slot in _slots)
            AddChild(slot);
    }

    public IElement? this[FormationSlot slot]
    {
        get => GetSlot(slot);
        set => SetSlot(slot, value);
    }

    public IElement? GetSlot(FormationSlot slot) => GetCharacterSlot(slot).Occupant;

    /// <summary>Assigns a detached element, or clears the slot when null.</summary>
    /// <remarks>Assign through this method rather than adding children directly.</remarks>
    public void SetSlot(FormationSlot slot, IElement? element)
    {
        var characterSlot = GetCharacterSlot(slot);
        if (ReferenceEquals(characterSlot.Occupant, element))
            return;

        if (element is not null)
        {
            if (ReferenceEquals(element, this))
                throw new ArgumentException("A formation cannot contain itself.", nameof(element));

            for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
                if (ReferenceEquals(ancestor, element))
                    throw new ArgumentException(
                        "A formation cannot contain an ancestor.",
                        nameof(element)
                    );

            foreach (var ownedSlot in _slots)
                if (ReferenceEquals(ownedSlot, element))
                    throw new ArgumentException(
                        "A formation slot cannot be an occupant.",
                        nameof(element)
                    );

            if (element.Parent is not null)
                throw new ArgumentException(
                    "Detach the element from its current parent first.",
                    nameof(element)
                );
        }

        characterSlot.SetOccupant(element);
        InvalidateLayout();
    }

    /// <summary>Changes one slot's normalized foot anchor, scale, and relative sort order.</summary>
    public void ConfigureSlot(
        FormationSlot slot,
        Vector2D<float> anchor,
        float scale,
        int sortOrder
    )
    {
        if (
            !float.IsFinite(anchor.X)
            || !float.IsFinite(anchor.Y)
            || anchor.X < 0f
            || anchor.X > 1f
            || anchor.Y < 0f
            || anchor.Y > 1f
        )
            throw new ArgumentOutOfRangeException(nameof(anchor));
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (sortOrder < -32768 || sortOrder > 32768)
            throw new ArgumentOutOfRangeException(nameof(sortOrder));

        var characterSlot = GetCharacterSlot(slot);
        characterSlot.Anchor = anchor;
        characterSlot.Scale = scale;
        characterSlot.SortOrder = sortOrder;
        InvalidateLayout();
    }

    private CharacterSlot GetCharacterSlot(FormationSlot slot)
    {
        var index = (int)slot;
        if ((uint)index >= (uint)_slots.Length)
            throw new ArgumentOutOfRangeException(nameof(slot));
        return _slots[index];
    }

    private class CharacterSlot : Element
    {
        private readonly CombatFormation _formation;
        public Vector2D<float> Anchor { get; set; }
        public float Scale { get; set; }
        private IElement? _occupant;

        public CharacterSlot(
            CombatFormation formation,
            int sortOrder,
            float x,
            float y,
            float scale
        )
        {
            _formation = formation;
            SortOrder = sortOrder;
            Anchor = new(x, y);
            Scale = scale;
        }

        public IElement? Occupant
        {
            get
            {
                if (_occupant is not null && !ReferenceEquals(_occupant.Parent, this))
                    _occupant = null;
                return _occupant;
            }
        }

        public void SetOccupant(IElement? element)
        {
            var previous = Occupant;
            if (element is not null)
                AddChild(element);
            _occupant = element;
            if (previous is not null)
                RemoveChild(previous);
        }

        public override void Arrange(Rectangle<float> bounds)
        {
            // The incoming rectangle is the formation's full content area.
            var width = bounds.Size.X * 0.24f * Scale;
            var height = bounds.Size.Y * 0.70f * Scale;
            var x = bounds.Origin.X + bounds.Size.X * Anchor.X;
            var y = bounds.Origin.Y + bounds.Size.Y * Anchor.Y;

            if (Occupant is { } element)
            {
                element.HorizontalAlignment = AlignHorizontal.Center;
                element.VerticalAlignment = AlignVertical.Bottom;
                element.SortOrder = (int)
                    Math.Clamp((long)_formation.SortOrder + SortOrder, -32768L, 32768L);
            }

            base.Arrange(new Rectangle<float>(x - width / 2f, y - height, width, height));
        }
    }
}
