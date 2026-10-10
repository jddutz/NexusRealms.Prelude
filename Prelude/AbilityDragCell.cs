using Nexus.Input.Events;

namespace NexusRealms.Prelude;

/// <summary>Captures an ability drag until release, including outside the source cell.</summary>
public sealed class AbilityDragCell(Action<Vector2D<float>> started,
    Action<Vector2D<float>> moved, Action<Vector2D<float>> dropped, Action? selected = null, Action? clicked = null) : Element
{
    private InputDeviceId? _pointer;
    private Vector2D<float> _origin;
    private bool _dragging;

    public void Handle(MouseButtonPressedEvent message)
    {
        if (message.Button != MouseButtonEnum.Left || _pointer is not null || !IsVisible
            || message.Position.X < Bounds.Origin.X || message.Position.X >= Bounds.Max.X
            || message.Position.Y < Bounds.Origin.Y || message.Position.Y >= Bounds.Max.Y) return;
        _pointer = message.Mouse.Id;
        _origin = message.Position;
        selected?.Invoke();
    }

    public void Handle(MouseMovedEvent message)
    {
        if (_pointer != message.Mouse.Id) return;
        var distance = message.Position - _origin;
        if (!_dragging && distance.X * distance.X + distance.Y * distance.Y >= 36f)
        {
            _dragging = true;
            started(message.Position);
        }
        if (_dragging) moved(message.Position);
    }

    public void Handle(MouseButtonReleasedEvent message)
    {
        if (message.Button != MouseButtonEnum.Left || _pointer != message.Mouse.Id) return;
        _pointer = null;
        if (_dragging) dropped(message.Position);
        else if (message.Position.X >= Bounds.Origin.X && message.Position.X < Bounds.Max.X
            && message.Position.Y >= Bounds.Origin.Y && message.Position.Y < Bounds.Max.Y) clicked?.Invoke();
        _dragging = false;
    }

    public void Handle(MouseDisconnectedEvent message)
    {
        if (_pointer != message.Mouse.Id) return;
        _pointer = null;
        if (_dragging) dropped(new(-1f, -1f));
        _dragging = false;
    }
}
