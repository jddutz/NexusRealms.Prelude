namespace NexusRealms.Prelude;

/// <summary>Displays the current outcome story node until its narrative is authored.</summary>
public sealed class OutcomeScene : Scene
{
    public OutcomeScene(GameState state, Storyline storyline, ITextStyleRegistry textStyles)
    {
        MainCamera = new StaticCamera();
        var node = storyline.Nodes[state.CurrentStoryNodeId] as OutcomeNode
            ?? throw new InvalidOperationException("Expected an outcome story node.");
        Children.Add(new TextElement(node.Title, textStyles.GetOrCreate(BuiltInFonts.Default, 32f))
        {
            Color = UiTheme.TextColor, Height = 48f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        });
        Children.Add(new View { Camera = MainCamera, LayerMask = Nexus.Graphics.RenderLayers.DefaultUI });
    }
}
