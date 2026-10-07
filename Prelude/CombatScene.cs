namespace NexusRealms.Prelude;

/// <summary>
/// Builds the opening combat encounter declared by the storyline.
/// </summary>
public class CombatScene : Scene
{
    private readonly CombatScenario _startScenario;
    private readonly Storyline _storyline;
    private readonly ITextureRegistry _textures;

    /// <summary>Creates the opening combat scene and its input bindings.</summary>
    /// <param name="textures">Provides background and character artwork.</param>
    /// <param name="eventHub">Dispatches the scene's input bindings.</param>
    /// <param name="windowService">Provides the main window closed by the exit bindings.</param>
    /// <param name="storyline">Provides the starting scenario and character definitions.</param>
    public CombatScene(
        ITextureRegistry textures,
        IEventHub eventHub,
        IWindowService windowService,
        Storyline storyline
    )
    {
        _textures = textures;
        _storyline = storyline;

        if (!storyline.Nodes.TryGetValue(storyline.StartNodeId, out var startNode))
        {
            throw new InvalidOperationException(
                $"The storyline start node '{storyline.StartNodeId}' is not registered."
            );
        }

        _startScenario =
            startNode as CombatScenario
            ?? throw new InvalidOperationException(
                $"The storyline start node '{storyline.StartNodeId}' is not a combat scenario."
            );

        MainCamera = new StaticCamera();

        var inputMap = new InputMap(eventHub);
        var window = windowService.GetMainWindow();
        inputMap.OnKeyPressed(KeyEnum.Escape).Invoke(() => window.Close());
        inputMap
            .OnAnyControllerButtonPressed(ControllerSemanticNames.Back)
            .Invoke(() => window.Close());
        InputMap = inputMap;
    }

    /// <summary>Initializes the background and character formation for the starting scenario.</summary>
    public override void Initialize()
    {
        if (IsInitialized)
            return;

        base.Initialize();

        Children.Add(new View { Camera = MainCamera, PreserveDrawOrder = true });
        Children.Add(
            new ImageElement
            {
                Texture = _textures.GetOrCreate(_startScenario.Background),
                SizingMode = ImageSizingMode.Fill,
                Margins = default,
                SortOrder = -32768,
            }
        );

        var formation = new CharacterFormation();
        foreach (var placement in _startScenario.Characters)
        {
            var character = _storyline.Characters[placement.CharacterId];
            formation.SetSlot(
                placement.Slot,
                new ImageElement
                {
                    Texture = _textures.GetOrCreate(character.Artwork),
                    SizingMode = ImageSizingMode.Fit,
                    Margins = default,
                }
            );
        }

        Children.Add(formation);
    }
}
