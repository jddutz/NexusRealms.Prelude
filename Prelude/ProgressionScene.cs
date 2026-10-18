namespace NexusRealms.Prelude;

/// <summary>Displays the player's Zodiac graph and learns abilities using available points.</summary>
public sealed class ProgressionScene : Scene
{
    private const float AbilityElementScale = 0.5f;
    private const float AbilityIconSize = 84f;
    private readonly List<(AbilityId Id, AbilityButton Button)> _buttons = [];
    private readonly TextElement _status;
    private readonly TextButton _continue;

    /// <summary>Creates the progression scene for the current story visit.</summary>
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
            SizingMode = ImageSizingMode.Stretch, SortOrder = -10,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        canvas.SetConnectors(connectors);
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
        var iconBackdrop = new Texture(new ContentId("ability-slot-backdrop"), 1, 1,
            [new Color(0f, 0f, 0f, 0.94f)]);
        foreach (var node in graph.Abilities)
        {
            if (node.Parent is { } parentId)
            {
                var parent = graph.Abilities.Single(candidate => candidate.Id == parentId);
                Edge(parent.X, parent.Y, node.X, node.Y);
            }
            var ability = story.Abilities[node.Id];
            var button = new AbilityButton(textStyles.GetOrCreate(BuiltInFonts.Bold, 13f))
            {
                Width = 168f * AbilityElementScale, Height = AbilityIconSize + 30f, SortOrder = 10,
                Padding = new(0f, 0f), RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                Action = _ =>
                {
                    ProgressionRules.Learn(state, story, node.Id);
                    Refresh();
                },
            };
            button.Icon = new ImageElement
            {
                Width = AbilityIconSize, Height = AbilityIconSize, SizingMode = ImageSizingMode.Stretch,
                HorizontalAlignment = AlignHorizontal.Center,
                VerticalAlignment = AlignVertical.Top,
                Margins = new(0f, 0f, 3f, 0f),
            };
            button.Icon.Children.Add(new ImageElement
            {
                Texture = iconBackdrop,
                Width = 78f, Height = 78f, SizingMode = ImageSizingMode.Stretch,
                HorizontalAlignment = AlignHorizontal.Center, VerticalAlignment = AlignVertical.Center,
                SortOrder = 11, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
            button.Frame = new ImageElement
            {
                Texture = textures.GetOrCreate(new ContentId("ui.item_frame.png")),
                Width = AbilityIconSize, Height = AbilityIconSize, SizingMode = ImageSizingMode.Stretch,
                SortOrder = 13, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            button.Icon.Children.Add(button.Frame);
            button.Glyph = new ImageElement
            {
                Texture = textures.GetOrCreate(ability.Icon),
                Width = AbilityIconSize, Height = AbilityIconSize, SizingMode = ImageSizingMode.Fit,
                HorizontalAlignment = AlignHorizontal.Center, VerticalAlignment = AlignVertical.Center,
                SortOrder = 12, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            button.Icon.Children.Add(button.Glyph);
            _buttons.Add((node.Id, button));
            canvas.Add(button, node.X, node.Y);
        }
        connectors.Texture = new Texture(new ContentId("progression-connectors"), connectorSize, connectorSize, connectorPixels);
        var zodiacTexture = textures.GetOrCreate(new ContentId($"zodiac.{state.Zodiac}.png"));
        canvas.SetArtwork(new ImageElement
        {
            Texture = zodiacTexture,
            Color = new Color(1f, 1f, 1f, 0.25f),
            SizingMode = ImageSizingMode.Stretch, SortOrder = -20,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        }, (float)zodiacTexture.Width / zodiacTexture.Height);
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
            StorySceneRouter.LoadCurrentNode(state, story, scenes);
        }, 140f);
        _continue.HorizontalAlignment = AlignHorizontal.Right;
        _continue.VerticalAlignment = AlignVertical.Bottom;
        _continue.Margins = new(0f, 16f, 0f, 18f);
        Children.Add(canvas);
        Children.Add(title);
        Children.Add(_status);
        Children.Add(_continue);
        var uiView = new View
        {
            Camera = MainCamera, LayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            PreserveDrawOrder = true,
        };
        uiView.ViewComponent.EnableDepthTest = false;
        uiView.ViewComponent.EnableDepthWrite = false;
        Children.Add(uiView);
        Refresh();

        void Refresh()
        {
            foreach (var (id, button) in _buttons)
            {
                var learned = state.Loadout.LearnedAbilities.Contains(id);
                var canLearn = ProgressionRules.CanLearn(state, story, id);
                button.IsEnabled = canLearn;
                button.Glyph.Color = new Color(1f, 1f, 1f, learned || canLearn ? 1f : 0.25f);
                button.Caption.Text = story.Abilities[id].Name;
                button.Frame.Texture = textures.GetOrCreate(new ContentId(learned
                    ? "ui.item_frame_selected.png" : "ui.item_frame.png"));
                button.Caption.Color = learned ? new Color(0.4f, 0.9f, 0.6f, 1f)
                    : canLearn ? UiTheme.TextColor : new Color(0.4f, 0.4f, 0.45f, 1f);
            }
            _status!.Text = $"Ability points: {state.AbilityPoints}. "
                + (state.AbilityPoints == 0 ? "No points available."
                    : ProgressionRules.HasLearnedThisVisit(state, visit) ? "Learn more abilities or continue when ready."
                    : "Choose an ability (1 point) or continue when ready. Prerequisites apply.");
            _continue!.IsVisible = ProgressionRules.CanContinue(state, visit);
        }
    }

    private sealed class AbilityButton : TextButton
    {
        /// <summary>Gets the ability name caption.</summary>
        public OutlinedHudText Caption { get; }

        /// <summary>Gets or sets the frame surrounding the ability glyph.</summary>
        public ImageElement Frame { get; set; } = null!;

        /// <summary>Gets or sets the ability glyph.</summary>
        public ImageElement Glyph { get; set; } = null!;
        private float _slotSize = AbilityIconSize;

        /// <summary>Updates the icon and button dimensions to match the available slot size.</summary>
        public void SetSlotSize(float size)
        {
            _slotSize = size;
            Height = size + 30f;
            Icon!.Width = Icon.Height = size;
            Frame.Width = Frame.Height = size;
            Glyph.Width = Glyph.Height = size;
        }

        /// <summary>Creates an ability button with a caption.</summary>
        public AbilityButton(ITextStyle style)
        {
            Caption = new OutlinedHudText(style)
            {
                Width = 168f, Height = 23f, SortOrder = 15,
                OutlineThickness = 1.5f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            Children.Add(Caption);
        }

        /// <inheritdoc/>
        public override Vector2D<float> Measure(Vector2D<float> constraint)
        {
            Caption.Measure(new Vector2D<float>(168f, 23f));
            return base.Measure(constraint);
        }

        /// <inheritdoc/>
        public override void Arrange(Rectangle<float> bounds)
        {
            base.Arrange(bounds);
            Caption.Arrange(new Rectangle<float>(Bounds.Origin.X + (Bounds.Size.X - 168f) / 2f,
                Bounds.Origin.Y + _slotSize + 7f * AbilityElementScale, 168f, 23f));
        }
    }

    private sealed class GraphCanvas : Element
    {
        private readonly List<(Element Element, float X, float Y)> _positions = [];
        private ImageElement? _connectors;
        private ImageElement? _artwork;
        private float _artworkAspectRatio = 1f;
        public void SetArtwork(ImageElement artwork, float aspectRatio)
        {
            _artwork = artwork;
            _artworkAspectRatio = aspectRatio;
            Children.Add(artwork);
        }
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
        public override Vector2D<float> Measure(Vector2D<float> constraint)
        {
            foreach (var (element, _, _) in _positions)
            {
                if (element is AbilityButton ability) ability.SetSlotSize(AbilityIconSize);
                element.Measure(new Vector2D<float>(element.Width ?? 100f, element.Height ?? 30f));
            }
            _connectors?.Measure(constraint);
            _artwork?.Measure(constraint);
            return constraint;
        }
        public override void Arrange(Rectangle<float> bounds)
        {
            base.Arrange(bounds);
            var artworkWidth = MathF.Min(Bounds.Size.X, Bounds.Size.Y * _artworkAspectRatio);
            var artworkHeight = artworkWidth / _artworkAspectRatio;
            var artworkBounds = new Rectangle<float>(
                Bounds.Origin.X + (Bounds.Size.X - artworkWidth) / 2f,
                Bounds.Origin.Y + (Bounds.Size.Y - artworkHeight) / 2f,
                artworkWidth, artworkHeight);
            _artwork?.Arrange(artworkBounds);
            _connectors?.Arrange(Bounds);
            foreach (var (element, x, y) in _positions)
            {
                var width = element.Width ?? 100f;
                var height = element.Height ?? 30f;
                element.Arrange(new Rectangle<float>(Bounds.Origin.X + Bounds.Size.X * (x + 1f) / 2f - width / 2f,
                    Bounds.Origin.Y + Bounds.Size.Y * (y + 1f) / 2f - (height - 30f) / 2f - 3f, width, height));
            }
        }
    }
}