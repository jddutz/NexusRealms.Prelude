global using Nexus.Core;
global using Nexus.Graphics;
global using Nexus.Graphics.Components;
global using Nexus.Graphics.Textures;
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
Check(starts == 0 && cell.CapturedMouse == mouse);
cell.Handle(new MouseMovedEvent(mouse, new(16f, 10f)));
Check(starts == 1);
cell.Handle(new MouseMovedEvent(mouse, new(200f, 200f)));
cell.Handle(new MouseButtonReleasedEvent(other, MouseButtonEnum.Left, new(200f, 200f)));
Check(starts == 1 && drops.Count == 0);
cell.Handle(new MouseButtonReleasedEvent(mouse, MouseButtonEnum.Left, new(200f, 200f)));
cell.Handle(new MouseButtonReleasedEvent(mouse, MouseButtonEnum.Left, new(200f, 200f)));
Check(clicks == 1 && cell.CapturedMouse is null);
Check(drops.Count == 1 && drops[0] == new Vector2D<float>(200f, 200f));
// Disconnect cancels with a position outside every valid drop target.
cell.Handle(new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(10f, 10f)));
cell.Handle(new MouseMovedEvent(mouse, new(30f, 30f)));
cell.Handle(new MouseDisconnectedEvent(mouse, new(30f, 30f)));
Check(drops.Count == 2 && drops[1] == new Vector2D<float>(-1f, -1f));
// Empty inventory/Throw targets must not treat another cell's release as their own click.
var sourceDrops = 0;
var targetClicks = 0;
var source = new AbilityDragCell(_ => { }, _ => { }, _ => sourceDrops++);
var target = new AbilityDragCell(_ => { }, _ => { }, _ => { }, clicked: () => targetClicks++);
source.Arrange(new(0f, 0f, 56f, 56f));
target.Arrange(new(100f, 0f, 56f, 56f));
var press = new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(20f, 20f));
source.Handle(press);
target.Handle(press);
source.Handle(new MouseMovedEvent(mouse, new(120f, 20f)));
target.Handle(new MouseMovedEvent(mouse, new(120f, 20f)));
var release = new MouseButtonReleasedEvent(mouse, MouseButtonEnum.Left, new(120f, 20f));
target.Handle(release); // Target dispatch first must not rebuild the dialog before the source drops.
source.Handle(release);
Check(sourceDrops == 1 && targetClicks == 0);
target.Handle(new MouseButtonPressedEvent(mouse, MouseButtonEnum.Left, new(120f, 20f)));
target.Handle(release);
Check(targetClicks == 1);
Console.WriteLine("Ability drag and empty-cell drop-target checks passed.");
// The preview is a graphics object and reads the latest mouse position directly every frame.
var texture = new Texture("drag-preview", 2, 1, [Colors.White, Colors.White]);
mouse.Position = new(80f, 90f);
var preview = new DragItem(texture, 42f, mouse);
Check((object)preview is not Element && preview.Position == mouse.Position);
Check(preview.Renderer.RenderLayerMask == RenderLayers.DefaultUI && preview.Renderer.DrawOrder == 30000);
var spriteInstance = preview.Renderer.Instances.Values.Single();
Check(spriteInstance.Size == new Vector2D<float>(42f, 21f) && spriteInstance.Anchor == new Vector2D<float>(0.5f, 0.5f));
var drawable = preview.Renderer.Drawables.Single();
mouse.Position = new(120f, 180f);
preview.Update(1.0 / 60.0);
Check(preview.Position == mouse.Position && ReferenceEquals(drawable, preview.Renderer.Drawables.Single()));
mouse.Position = new(400f, 210f);
preview.FollowMouse();
Check(preview.Position == mouse.Position);
mouse.IsConnected = false;
preview.Update(1.0 / 60.0);
Check(!preview.Renderer.IsVisible);
Console.WriteLine("Drag sprite follows the latest mouse position without GUI layout, preserves aspect ratio and hides on disconnect.");

// Replacing an inventory icon retains its renderer and geometry without invalidating layout.
var itemSlot = new ItemSlot { Width = 56f, Height = 56f };
itemSlot.Arrange(new(10f, 20f, 56f, 56f));
var invalidations = 0;
itemSlot.PropertyChanged += _ => invalidations++;
itemSlot.SetIcon(texture, false);
var iconRenderer = itemSlot.Components.OfType<TextureRenderer>().Single();
var iconDrawable = iconRenderer.Drawables.Single();
Check(iconRenderer.Destination == new Rectangle<float>(17f, 37.5f, 42f, 21f));
var tallTexture = new Texture("tall-slot", 1, 2, [Colors.White, Colors.White]);
itemSlot.SetIcon(tallTexture, false);
Check(ReferenceEquals(iconDrawable, iconRenderer.Drawables.Single()));
Check(iconRenderer.Destination == new Rectangle<float>(27.5f, 27f, 21f, 42f));
itemSlot.SetIcon(null, false);
Check(!iconRenderer.IsVisible);
itemSlot.SetIcon(texture, true);
Check(iconRenderer.IsVisible && invalidations == 0 && itemSlot.Children.Count == 0);
Console.WriteLine("Slot icons update in place, preserve aspect ratio, and do not invalidate layout.");

sealed class TestMouse(ulong id) : IMouseInputDevice
{
    public InputDeviceId Id { get; } = new(id);
    public string Name => "Test mouse";
    public bool IsConnected { get; set; } = true;
    public Vector2D<float> Position { get; set; }
    public bool IsButtonDown(MouseButtonEnum button) => false;
    public event Action<IMouseInputDevice, Vector2D<float>>? Moved { add { } remove { } }
    public event Action<IMouseInputDevice, MouseButtonEnum>? ButtonPressed { add { } remove { } }
    public event Action<IMouseInputDevice, MouseButtonEnum>? ButtonReleased { add { } remove { } }
    public event Action<IMouseInputDevice, Vector2D<float>>? WheelMoved { add { } remove { } }
}
