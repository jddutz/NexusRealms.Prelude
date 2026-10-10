global using Nexus.GUI.Elements;
global using Nexus.Input;
global using Nexus.Input.Devices;
global using Silk.NET.Maths;
using Nexus.Input.Events;
using NexusRealms.Prelude;

static void Check(bool value) { if (!value) throw new Exception("Drag check failed"); }
var mouse = new TestMouse(1);
var other = new TestMouse(2);
var starts = 0;
var selections = 0;
var clicks = 0;
var drops = new List<Vector2D<float>>();
var cell = new AbilityDragCell(_ => starts++, _ => { }, drops.Add, () => selections++, () => clicks++);
cell.Arrange(new(0f, 0f, 100f, 60f));
// Clicking is not a drag, and starting outside the cell is ignored.
cell.Handle(new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(10f, 10f)));
cell.Handle(new MouseButtonReleasedEvent(mouse, MouseButtonEnum.Left, new(10f, 10f)));
Check(starts == 0 && drops.Count == 0 && selections == 1 && clicks == 1);
cell.Handle(new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(110f, 10f)));
cell.Handle(new MouseMovedEvent(mouse, new(200f, 200f)));
Check(starts == 0);
// Capture follows the source mouse outside the cell, and releases once.
cell.Handle(new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(10f, 10f)));
cell.Handle(new MouseMovedEvent(mouse, new(12f, 10f)));
Check(starts == 0);
cell.Handle(new MouseMovedEvent(mouse, new(200f, 200f)));
cell.Handle(new MouseButtonReleasedEvent(other, MouseButtonEnum.Left, new(200f, 200f)));
Check(starts == 1 && drops.Count == 0);
cell.Handle(new MouseButtonReleasedEvent(mouse, MouseButtonEnum.Left, new(200f, 200f)));
cell.Handle(new MouseButtonReleasedEvent(mouse, MouseButtonEnum.Left, new(200f, 200f)));
Check(clicks == 1);
Check(drops.Count == 1 && drops[0] == new Vector2D<float>(200f, 200f));
// Disconnect cancels with a position outside every valid drop target.
cell.Handle(new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(10f, 10f)));
cell.Handle(new MouseMovedEvent(mouse, new(30f, 30f)));
cell.Handle(new MouseDisconnectedEvent(mouse, new(30f, 30f)));
Check(drops.Count == 2 && drops[1] == new Vector2D<float>(-1f, -1f));
Console.WriteLine("Ability drag checks passed.");

sealed class TestMouse(ulong id) : IMouseInputDevice
{
    public InputDeviceId Id { get; } = new(id);
    public string Name => "Test mouse";
    public bool IsConnected => true;
    public Vector2D<float> Position => default;
    public bool IsButtonDown(MouseButtonEnum button) => false;
    public event Action<IMouseInputDevice, Vector2D<float>>? Moved { add { } remove { } }
    public event Action<IMouseInputDevice, MouseButtonEnum>? ButtonPressed { add { } remove { } }
    public event Action<IMouseInputDevice, MouseButtonEnum>? ButtonReleased { add { } remove { } }
    public event Action<IMouseInputDevice, Vector2D<float>>? WheelMoved { add { } remove { } }
}
