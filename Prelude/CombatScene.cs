namespace NexusRealms.Prelude;

/// <summary>
/// Builds the HelloNexus welcome screen and its application-specific content.
/// </summary>
public class CombatScene : Scene
{
    private readonly ITextStyleRegistry _textStyles;
    private readonly ITextureRegistry _textures;

    /// <summary>Creates the welcome screen with its camera, input bindings, and content services.</summary>
    /// <param name="textStyles">Builds and caches text styles for the welcome text.</param>
    /// <param name="eventHub">Dispatches the welcome screen's input bindings.</param>
    /// <param name="windowService">Provides the main window closed by the exit bindings.</param>
    public CombatScene(
        ITextStyleRegistry textStyles,
        ITextureRegistry textures,
        IEventHub eventHub,
        IWindowService windowService
    )
    {
        _textStyles = textStyles;
        _textures = textures;

        MainCamera = new StaticCamera();

        var inputMap = new InputMap(eventHub);
        var window = windowService.GetMainWindow();
        inputMap.OnKeyPressed(KeyEnum.Escape).Invoke(() => window.Close());
        inputMap
            .OnAnyControllerButtonPressed(ControllerSemanticNames.Back)
            .Invoke(() => window.Close());
        InputMap = inputMap;
    }

    /// <summary>Initializes the welcome screen's visual hierarchy.</summary>
    public override void Initialize()
    {
        base.Initialize();

        var textStyleSmall = _textStyles.GetOrCreate(BuiltInFonts.Default, 10);
        var textStyleLarge = _textStyles.GetOrCreate(BuiltInFonts.Default, 32);

        Children.Add(new View { Camera = MainCamera, PreserveDrawOrder = true });
        Children.Add(
            new ImageElement
            {
                Texture = _textures.GetOrCreate("alley_1"),
                SizingMode = ImageSizingMode.Fill,
                Margins = default,
                SortOrder = -32768,
            }
        );

        Children.Add(
            new ImageElement
            {
                Texture = _textures.GetOrCreate("alley_thug_1"),
                SizingMode = ImageSizingMode.Fill,
                Height = 500,
                Width = 300,
                Margins = default,
                SortOrder = -32768,
            }
        );
        Children.Add(
            new ImageElement
            {
                Texture = _textures.GetOrCreate("alley_thug_2"),
                SizingMode = ImageSizingMode.Fill,
                Height = 450,
                Width = 270,
                Margins = default,
                SortOrder = -32768,
            }
        );
        Children.Add(
            new ImageElement
            {
                Texture = _textures.GetOrCreate("alley_thug_3"),
                SizingMode = ImageSizingMode.Fill,
                Height = 450,
                Width = 270,
                Margins = default,
                SortOrder = -32768,
            }
        );
    }
}
