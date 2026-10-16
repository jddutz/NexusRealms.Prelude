namespace NexusRealms.Prelude;

/// <summary>Displays the player's Zodiac graph and learns one ability per story visit.</summary>
public sealed class ProgressionScene : Scene
{
    private readonly List<(AbilityId Id, TextButton Button)> _buttons = [];
    private readonly TextElement _status;
    private readonly TextButton _continue;

    public ProgressionScene(GameState state, Storyline story, ITextStyleRegistry textStyles,
        ITextureRegistry textures, ISceneManager scenes)
    {
        MainCamera = new StaticCamera();
        var graph = Database.Progression.ProgressionGraphs.All[state.Zodiac];
        var visit = story.Nodes[state.CurrentStoryNodeId] as ProgressNode
            ?? throw new InvalidOperationException("Expected a progression story node.");
        var style = textStyles.GetOrCreate(BuiltInFonts.Default, 16f);
        var canvas = new GraphCanvas { Margins = new(12f, 12f, 55f, 70f) };
        // All connectors share one image/renderer rather than allocating a drawable per dot.
        const int connectorSize = 1024;
        var connectorPixels = new Color[connectorSize * connectorSize];
        var connectors = new ImageElement
        {
            SizingMode = ImageSizingMode.Stretch,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        canvas.SetConnectors(connectors);
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
        void Edge(float ax, float ay, float bx, float by)
        {
            for (var i = 1; i < 20; i++)
            {
                var t = i / 20f;
                var x = (int)((ax + (bx - ax) * t + 1f) * (connectorSize - 1) / 2f);
                var y = (int)((ay + (by - ay) * t + 1f) * (connectorSize - 1) / 2f);
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                        if (x + dx >= 0 && x + dx < connectorSize && y + dy >= 0 && y + dy < connectorSize)
                            connectorPixels[(y + dy) * connectorSize + x + dx] = new Color(0.5f, 0.65f, 0.85f, 1f);
            }
        }
        foreach (var direction in Enum.GetValues<DirectionEnum>())
        {
            var root = roots[(int)direction];
            Edge(0f, 0f, root.X, root.Y);
            var rootLabel = Label(direction.ToString().ToUpperInvariant());
            rootLabel.Width = 65f;
            canvas.Add(rootLabel, root.X, root.Y);
            foreach (var node in graph.Abilities.Where(node => node.Direction == direction))
            {
                var parent = graph.Abilities.FirstOrDefault(candidate => candidate.Id == node.Parent);
                Edge(parent?.X ?? root.X, parent?.Y ?? root.Y, node.X, node.Y);
                var button = Button(story.Abilities[node.Id].Name, () =>
                {
                    ProgressionRules.Learn(state, story, node.Id);
                    Refresh();
                }, 100f);
                _buttons.Add((node.Id, button));
                canvas.Add(button, node.X, node.Y);
            }
        }
        connectors.Texture = new Texture(new ContentId("progression-connectors"), connectorSize, connectorSize, connectorPixels);
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
        var title = Label($"Chapter {visit.Chapter} — {state.Zodiac}");
        title.Width = 400f;
        title.VerticalAlignment = AlignVertical.Top;
        title.Margins = new(16f, 0f, 12f, 0f);
        _status = Label("");
        _status.Width = 650f;
        _status.VerticalAlignment = AlignVertical.Bottom;
        _status.Margins = new(16f, 0f, 0f, 18f);
        _continue = Button("Continue", () =>
        {
            if (!ProgressionRules.CanContinue(state, visit)) return;
            state.CurrentStoryNodeId = visit.NextNode;
            if (story.Nodes[state.CurrentStoryNodeId] is ProgressNode) scenes.LoadScene<ProgressionScene>();
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
                button.IsEnabled = ProgressionRules.CanLearn(state, story, id);
                button.Label = (learned ? "+ " : "") + story.Abilities[id].Name;
                button.TextColor = learned ? new Color(0.4f, 0.9f, 0.6f, 1f)
                    : ProgressionRules.CanLearn(state, story, id) ? UiTheme.TextColor : new Color(0.4f, 0.4f, 0.45f, 1f);
            }
            _status!.Text = $"Ability points: {state.AbilityPoints}. "
                + (ProgressionRules.HasLearnedThisVisit(state, visit) ? "Ability learned. Continue when ready."
                    : state.AbilityPoints == 0 ? "No points available."
                    : "Choose an ability (1 point). Prerequisites apply.");
            _continue!.IsVisible = ProgressionRules.HasLearnedThisVisit(state, visit);
        }
    }

    private sealed class GraphCanvas : Element
    {
        private readonly List<(Element Element, float X, float Y)> _positions = [];
        private ImageElement? _connectors;
        public void SetConnectors(ImageElement connectors)
        {
            _connectors = connectors;
            Children.Add(connectors);
        }
        public void Add(Element element, float x, float y)
        {
            Children.Add(element);
            _positions.Add((element, x, y));
        }
        public override Vector2D<float> Measure(Vector2D<float> constraint) => constraint;
        public override void Arrange(Rectangle<float> bounds)
        {
            base.Arrange(bounds);
            _connectors?.Arrange(Bounds);
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