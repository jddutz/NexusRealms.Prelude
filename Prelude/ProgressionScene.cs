namespace NexusRealms.Prelude;

/// <summary>Displays the player's Zodiac graph and learns one ability per story visit.</summary>
public sealed class ProgressionScene : Scene
{
    private readonly ProgressionSession _session;
    private readonly List<(AbilityId Id, TextButton Button)> _buttons = [];
    private readonly List<(ProgressionBranch Branch, TextElement Label)> _branches = [];
    private readonly TextElement _status;
    private readonly TextButton _continue;

    public ProgressionScene(GameState state, Storyline story, ITextStyleRegistry textStyles,
        ITextureRegistry textures, ISceneManager scenes)
    {
        MainCamera = new StaticCamera();
        _session = new(state, story);
        var style = textStyles.GetOrCreate(BuiltInFonts.Default, 16f);
        var canvas = new GraphCanvas { Margins = new(12f, 12f, 55f, 70f) };
        var pixel = new Texture(new ContentId("progression-line"), 1, 1, [Colors.White]);
        var roots = new (float X, float Y)[] { (0f, -0.23f), (0.23f, 0f), (0f, 0.23f), (-0.23f, 0f) };
        TextElement Label(string text) => new(text, style)
        {
            Color = UiTheme.TextColor, Height = 30f, Width = 200f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        TextButton Button(string text, Action action, float width) => new()
        {
            Label = text, Style = style, TextColor = UiTheme.TextColor, Width = width, Height = 30f,
            Texture = textures.GetOrCreate(new ContentId("ui.panel_small.png")),
            SourceBorders = new(48f, 32f, 48f, 32f), BorderScale = 0.2f,
            Padding = new(3f, 2f), RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            Action = _ => action(), SortOrder = 2,
        };
        void Edge(float ax, float ay, float bx, float by, bool unlocked)
        {
            // Small texture marks form connectors without requiring rotated GUI elements.
            for (var i = 1; i < 20; i++)
            {
                var t = i / 20f;
                canvas.Add(new ImageElement
                {
                    Texture = pixel, Width = 3f, Height = 3f,
                    Color = unlocked ? new Color(0.5f, 0.65f, 0.85f, 1f) : new Color(0.2f, 0.23f, 0.28f, 1f),
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                }, ax + (bx - ax) * t, ay + (by - ay) * t);
            }
        }
        foreach (var branch in _session.Graph.Branches)
        {
            var root = roots[(int)branch.Direction];
            var unlocked = _session.IsUnlocked(branch.Direction);
            Edge(0f, 0f, root.X, root.Y, unlocked);
            var rootLabel = Label(branch.Direction.ToString().ToUpperInvariant());
            rootLabel.Width = 65f;
            _branches.Add((branch.Direction, rootLabel));
            canvas.Add(rootLabel, root.X, root.Y);
            foreach (var node in branch.Abilities)
            {
                var parent = branch.Abilities.FirstOrDefault(candidate => candidate.Ability == node.Parent);
                Edge(parent?.X ?? root.X, parent?.Y ?? root.Y, node.X, node.Y, unlocked);
                var button = Button(story.Abilities[node.Ability].Name, () =>
                {
                    _session.Learn(node.Ability);
                    Refresh();
                }, 100f);
                _buttons.Add((node.Ability, button));
                canvas.Add(button, node.X, node.Y);
            }
        }
        Rectangle<int>[] regions =
        [
            new(0, 0, 375, 315), new(420, 0, 380, 315), new(803, 0, 393, 340), new(1200, 0, 364, 315),
            new(0, 323, 425, 334), new(445, 323, 366, 369), new(830, 323, 360, 344), new(1200, 323, 364, 344),
            new(0, 638, 435, 368), new(435, 661, 381, 345), new(825, 662, 377, 344), new(1202, 666, 362, 340),
        ];
        canvas.Add(new ImageElement
        {
            Texture = textures.GetOrCreate(new ContentId("zodiac_signs")), SourceRegion = regions[(int)state.Zodiac],
            Width = 70f, Height = 70f, SizingMode = ImageSizingMode.Fit, SortOrder = 3,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        }, 0f, 0f);
        var title = Label($"Chapter {_session.Visit.Chapter} — {state.Zodiac}");
        title.Width = 400f;
        title.VerticalAlignment = AlignVertical.Top;
        title.Margins = new(16f, 0f, 12f, 0f);
        _status = Label("");
        _status.Width = 650f;
        _status.VerticalAlignment = AlignVertical.Bottom;
        _status.Margins = new(16f, 0f, 0f, 18f);
        _continue = Button("Continue", () =>
        {
            if (!_session.Continue()) return;
            if (story.Nodes[state.CurrentStoryNodeId] is ProgressionNode) scenes.LoadScene<ProgressionScene>();
            else scenes.LoadScene<CombatScene>();
        }, 140f);
        _continue.HorizontalAlignment = AlignHorizontal.Right;
        _continue.VerticalAlignment = AlignVertical.Bottom;
        _continue.Margins = new(0f, 16f, 0f, 18f);
        Children.Add(canvas);
        Children.Add(title);
        Children.Add(_status);
        Children.Add(_continue);
        Children.Add(new View { Camera = MainCamera, LayerMask = Nexus.Graphics.RenderLayers.DefaultUI });
        Refresh();

        void Refresh()
        {
            foreach (var (id, button) in _buttons)
            {
                var learned = state.Loadout.LearnedAbilities.Contains(id);
                button.IsEnabled = _session.CanLearn(id);
                button.Label = (learned ? "+ " : "") + story.Abilities[id].Name;
                button.TextColor = learned ? new Color(0.4f, 0.9f, 0.6f, 1f)
                    : _session.CanLearn(id) ? UiTheme.TextColor : new Color(0.4f, 0.4f, 0.45f, 1f);
            }
            foreach (var (branch, label) in _branches)
                label.Color = _session.IsUnlocked(branch) ? UiTheme.TextColor : new Color(0.4f, 0.4f, 0.45f, 1f);
            _status!.Text = _session.HasLearnedThisVisit ? "Ability learned. Continue to the next story node."
                : "Choose one available ability. Learn its connected parent first.";
            _continue!.IsVisible = _session.HasLearnedThisVisit;
        }
    }

    private sealed class GraphCanvas : Element
    {
        private readonly List<(Element Element, float X, float Y)> _positions = [];
        public void Add(Element element, float x, float y)
        {
            Children.Add(element);
            _positions.Add((element, x, y));
        }
        public override Vector2D<float> Measure(Vector2D<float> constraint) => constraint;
        public override void Arrange(Rectangle<float> bounds)
        {
            base.Arrange(bounds);
            foreach (var (element, x, y) in _positions)
            {
                var width = element.Width ?? 100f;
                var height = element.Height ?? 30f;
                element.Arrange(new Rectangle<float>(Bounds.Origin.X + Bounds.Size.X * (x + 1f) / 2f - width / 2f,
                    Bounds.Origin.Y + Bounds.Size.Y * (y + 1f) / 2f - height / 2f, width, height));
            }
        }
    }
}