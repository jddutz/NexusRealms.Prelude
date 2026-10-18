namespace NexusRealms.Prelude;

/// <summary>Displays a narrative node's artwork, rich text, and branching choices.</summary>
public sealed class NarrativeScene : Scene
{
    /// <summary>Creates the scene for the current narrative node.</summary>
    /// <param name="state">The mutable story state updated when a choice is selected.</param>
    /// <param name="storyline">The registered story nodes used to resolve destinations.</param>
    /// <param name="textStyles">Provides text styles for narrative copy and choice labels.</param>
    /// <param name="textures">Provides the authored artwork and choice panel textures.</param>
    /// <param name="scenes">Loads the scene associated with a selected destination.</param>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    /// <exception cref="InvalidOperationException">The current node is not a narrative node.</exception>
    public NarrativeScene(
        GameState state,
        StoryRegistry storyline,
        ITextStyleRegistry textStyles,
        ITextureRegistry textures,
        ISceneManager scenes
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(storyline);
        ArgumentNullException.ThrowIfNull(textStyles);
        ArgumentNullException.ThrowIfNull(textures);
        ArgumentNullException.ThrowIfNull(scenes);

        MainCamera = new StaticCamera();
        var node = storyline.Nodes[state.CurrentStoryNodeId] as NarrativeNode
            ?? throw new InvalidOperationException("Expected a narrative story node.");
        var renderLayer = Nexus.Graphics.RenderLayers.DefaultUI;
        var background = new ImageElement
        {
            Texture = textures.GetOrCreate(node.BackgroundImage),
            SizingMode = ImageSizingMode.Stretch,
            SortOrder = -100,
            RenderLayerMask = renderLayer,
        };
        var foreground = new ImageElement
        {
            Texture = textures.GetOrCreate(node.ForegroundImage),
            SizingMode = ImageSizingMode.Fit,
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Center,
            RenderLayerMask = renderLayer,
        };
        var fontSize = 20f;
        var richText = new RichTextElement(
            node.RichText,
            textStyles.GetOrCreate(BuiltInFonts.Default, fontSize),
            textStyles.GetOrCreate(BuiltInFonts.Bold, fontSize),
            textStyles.GetOrCreate(BuiltInFonts.Italic, fontSize),
            textStyles.GetOrCreate(BuiltInFonts.BoldItalic, fontSize),
            UiTheme.TextColor,
            renderLayer
        )
        {
            Margins = new(24f),
            HorizontalAlignment = AlignHorizontal.Left,
            VerticalAlignment = AlignVertical.Center,
        };
        var main = new GridLayout
        {
            Rows = [GridSize.Relative()],
            Columns = [GridSize.Relative(), GridSize.Relative()],
        };
        main[0, 0] = foreground;
        main[0, 1] = richText;

        var choices = new GridLayout
        {
            Rows = Enumerable.Repeat(GridSize.Relative(), node.Choices.Length).ToArray(),
            Columns = [GridSize.Relative()],
            Margins = new(8f, 8f, 4f, 4f),
        };
        var buttonStyle = textStyles.GetOrCreate(BuiltInFonts.Default, 18f);
        var buttonTexture = textures.GetOrCreate(new ContentId("ui.panel_small.png"));
        for (var index = 0; index < node.Choices.Length; index++)
        {
            var choice = node.Choices[index];
            choices[index, 0] = new TextButton
            {
                Label = choice.Text,
                Style = buttonStyle,
                TextColor = UiTheme.TextColor,
                Texture = buttonTexture,
                SourceBorders = new(48f, 32f, 48f, 32f),
                BorderScale = 0.2f,
                Width = 600f,
                Height = 48f,
                Margins = new(6f),
                RenderLayerMask = renderLayer,
                Action = _ =>
                {
                    state.CurrentStoryNodeId = choice.NextNode;
                    StorySceneRouter.LoadCurrentNode(state, storyline, scenes);
                },
            };
        }

        var layout = new GridLayout
        {
            Rows = [GridSize.Relative(), GridSize.Absolute(Math.Min(320f, node.Choices.Length * 60f))],
            Columns = [GridSize.Relative()],
            Margins = new(24f),
        };
        layout[0, 0] = main;
        layout[1, 0] = choices;
        Children.Add(background);
        Children.Add(layout);

        var uiView = new View
        {
            Camera = MainCamera,
            LayerMask = renderLayer,
            PreserveDrawOrder = true,
        };
        uiView.ViewComponent.EnableDepthTest = false;
        uiView.ViewComponent.EnableDepthWrite = false;
        Children.Add(uiView);
    }
}
