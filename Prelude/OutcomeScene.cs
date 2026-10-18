namespace NexusRealms.Prelude;

/// <summary>Displays a combat outcome and offers its optional continuation.</summary>
public sealed class OutcomeScene : Scene
{
    /// <summary>Creates the scene for the current outcome node.</summary>
    /// <param name="state">The mutable game state updated when an outcome is continued.</param>
    /// <param name="storyline">The registered graph used to resolve the next node.</param>
    /// <param name="textStyles">Provides styles for the title and continuation button.</param>
    /// <param name="textures">Provides the continuation button background.</param>
    /// <param name="scenes">Loads the scene associated with the next node.</param>
    public OutcomeScene(
        GameState state,
        StoryRegistry storyline,
        ITextStyleRegistry textStyles,
        ITextureRegistry textures,
        ISceneManager scenes
    )
    {
        MainCamera = new StaticCamera();
        var node = storyline.Nodes[state.CurrentStoryNodeId] as OutcomeNode
            ?? throw new InvalidOperationException("Expected an outcome story node.");
        Children.Add(new TextElement(node.Title, textStyles.GetOrCreate(BuiltInFonts.Default, 32f))
        {
            Color = UiTheme.TextColor,
            Height = 48f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        });
        if (node.NextNode is { } nextNode)
        {
            Children.Add(new TextButton
            {
                Label = "Continue",
                Style = textStyles.GetOrCreate(BuiltInFonts.Default, 18f),
                TextColor = UiTheme.TextColor,
                Texture = textures.GetOrCreate(new ContentId("ui.panel_small.png")),
                SourceBorders = new(48f, 32f, 48f, 32f),
                BorderScale = 0.2f,
                Width = 160f,
                Height = 48f,
                HorizontalAlignment = AlignHorizontal.Right,
                VerticalAlignment = AlignVertical.Bottom,
                Margins = new(16f),
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                Action = _ =>
                {
                    state.CurrentStoryNodeId = nextNode;
                    StorySceneRouter.LoadCurrentNode(state, storyline, scenes);
                },
            });
        }
        Children.Add(new View { Camera = MainCamera, LayerMask = Nexus.Graphics.RenderLayers.DefaultUI });
    }
}
