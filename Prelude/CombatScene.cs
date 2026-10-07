namespace NexusRealms.Prelude;

/// <summary>
/// Builds the opening combat encounter declared by the storyline.
/// </summary>
public class CombatScene : Scene
{
    private const float SideHudWidth = 240f;
    private const float TopHudHeight = 120f;
    private const float BottomHudHeight = 210f;
    private const float WorldViewBottomGap = 120f;
    private readonly CombatScenario _startScenario;
    private readonly Storyline _storyline;
    private readonly ITextureRegistry _textures;
    private readonly IWindowService _windowService;
    private readonly OrthoCamera _worldCamera = new();
    private readonly SceneBackground _background;
    private readonly CharacterFormation _formation;
    private readonly ulong _worldLayerMask;
    private readonly TextButton _retreatButton;
    private readonly Vector2D<float> _worldSize;
    private readonly GameState _gameState;
    private readonly ITextStyle _hudTextStyle;

    /// <summary>Creates the opening combat scene and its input bindings.</summary>
    /// <param name="textures">Provides background and character artwork.</param>
    /// <param name="eventHub">Dispatches the scene's input bindings.</param>
    /// <param name="windowService">Provides the main window closed by the exit bindings.</param>
    /// <param name="storyline">Provides the starting scenario and character definitions.</param>
    /// <param name="textStyles">Provides the style used by the retreat button.</param>
    public CombatScene(
        ITextureRegistry textures,
        IEventHub eventHub,
        IWindowService windowService,
        Storyline storyline,
        ITextStyleRegistry textStyles,
        GameState gameState
    )
    {
        _textures = textures;
        _storyline = storyline;
        _windowService = windowService;
        _gameState = gameState;
        _hudTextStyle = textStyles.GetOrCreate(BuiltInFonts.Default, 16f);

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
        _worldLayerMask = 1UL << RenderLayers.Create("World", RenderPasses.Main).Index;
        _background = new SceneBackground(
            _textures.GetOrCreate(_startScenario.Background),
            _worldLayerMask
        );
        _worldSize = new(_background.Texture.Width, _background.Texture.Height);
        _formation = new CharacterFormation(_worldSize);
        var uiAtlas = _textures.GetOrCreate(new ContentId("ui.Weathered bronze and copper UI atlas.png"));
        var retreatRegion = uiAtlas.GetRegion("region-0008").TexCoords;
        _retreatButton = new TextButton
        {
            Label = "Retreat",
            Width = 120f,
            Height = 54f,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Top,
            Style = textStyles.GetOrCreate(BuiltInFonts.Default, 16f),
            Texture = uiAtlas,
            TexCoord = new(
                retreatRegion.Origin.X, retreatRegion.Origin.Y,
                retreatRegion.Size.X, retreatRegion.Size.Y),
            SourceBorders = new(48f, 32f, 48f, 32f),
            BorderScale = 0.4f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            Padding = new(10f, 8f),
            LabelHorizontalAlignment = AlignHorizontal.Left,
            LabelVerticalAlignment = AlignVertical.Center,
            LabelMargins = new(36f, 0f, 0f, 0f),
            Icon = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("icons.retreat.png")),
                Width = 24f,
                Height = 24f,
                SizingMode = ImageSizingMode.Fit,
                HorizontalAlignment = AlignHorizontal.Left,
                VerticalAlignment = AlignVertical.Center,
                Margins = new(6f, 0f, 0f, 0f),
                Color = new Color(0.7f, 0.7f, 0.7f),
            },
            Margins = new(0f, 10f, 10f, 0f),
        };
        foreach (var placement in _startScenario.Characters)
        {
            var character = _storyline.Characters[placement.CharacterId];
            var combatCharacter = new CombatCharacter(
                character,
                _textures.GetOrCreate(character.Artwork)
            );
            combatCharacter.Renderer.RenderLayerMask = _worldLayerMask;
            _formation.SetSlot(placement.Slot, combatCharacter);
        }

        var inputMap = new InputMap(eventHub);
        inputMap.OnKeyPressed(KeyEnum.Escape).Invoke(() => _windowService.GetMainWindow().Close());
        inputMap
            .OnAnyControllerButtonPressed(ControllerSemanticNames.Back)
            .Invoke(() => _windowService.GetMainWindow().Close());
        InputMap = inputMap;
    }

    /// <summary>Initializes the background and character formation for the starting scenario.</summary>
    public override void Initialize()
    {
        if (IsInitialized)
            return;

        base.Initialize();

        var worldView = new View
        {
            Camera = _worldCamera,
            LayerMask = _worldLayerMask,
            PreserveDrawOrder = true,
            SizingMode = ViewSizingMode.Fill,
        };
        var viewLayout = new GridLayout
        {
            SortOrder = -100,
            Rows = [GridSize.Relative(), GridSize.Absolute(WorldViewBottomGap)],
            Columns = [GridSize.Relative()],
        };
        worldView.SortOrder = -100;
        viewLayout.SetCell(0, 0, worldView);
        var layout = new GridLayout
        {
            Rows =
            [
                GridSize.Absolute(TopHudHeight),
                GridSize.Relative(),
                GridSize.Absolute(BottomHudHeight),
            ],
            Columns =
            [
                GridSize.Absolute(SideHudWidth),
                GridSize.Relative(),
                GridSize.Absolute(SideHudWidth),
            ],
        };

        var atlas = _textures.GetOrCreate(new ContentId("ui.Weathered bronze and copper UI atlas.png"));
        layout.SetCell(0, 2, _retreatButton);
        var turnOrder = new TurnOrderStrip
        {
            Width = (1 + _startScenario.Characters.Length) * 80f + _startScenario.Characters.Length * 8f,
            Height = 96f,
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Top,
            Margins = new(0f, 0f, 10f, 0f),
        };
        turnOrder.Items.Children.Add(new TurnOrderPortrait(_textures.GetOrCreate(_gameState.Portrait), atlas)
        {
            IsActive = true,
        });
        foreach (var placement in _startScenario.Characters)
        {
            turnOrder.Items.Children.Add(new ImageElement
            {
                Texture = atlas,
                SourceRegion = atlas.GetRegion("region-0003").Bounds,
                Width = 8f,
                Height = 8f,
                VerticalAlignment = AlignVertical.Top,
                Margins = new(0f, 0f, 34f, 0f),
                SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
            turnOrder.Items.Children.Add(new TurnOrderPortrait(
                _textures.GetOrCreate(_storyline.Characters[placement.CharacterId].Portrait), atlas));
        }
        layout.SetCell(0, 1, turnOrder);
        var bottomLeft = CreateAtlasPanel(atlas, "region-0000");
        bottomLeft.IsSelected = true;
        bottomLeft.Margins = new(10f, 10f);
        // The ice spell occupies the second cell in the sheet's first row.
        bottomLeft.Children.Add(CreateHudImage("abilities.basic_spells.png", new(410, 0, 397, 334)));
        bottomLeft.Children.Add(CreateHudLabel("Right Hand", AlignVertical.Top));
        bottomLeft.Children.Add(CreateHudLabel("Frost", AlignVertical.Bottom));
        layout.SetCell(2, 0, bottomLeft);
        var bottomCenter = CreateAtlasPanel(atlas, "region-0006");
        bottomCenter.Height = 128f;
        bottomCenter.Margins = new(3f, 3f, 3f, 10f);
        bottomCenter.VerticalAlignment = AlignVertical.Bottom;
        bottomCenter.Children.Add(new PlayerHudPortrait(_textures.GetOrCreate(_gameState.Portrait)));
        layout.SetCell(2, 1, bottomCenter);
        var bottomRight = CreateAtlasPanel(atlas, "region-0000");
        bottomRight.Margins = new(10f, 10f);
        // The sword occupies the first cell in the sheet's first row.
        bottomRight.Children.Add(CreateHudImage("equipment.weapons_one_handed.png", new(0, 0, 405, 334)));
        bottomRight.Children.Add(CreateHudLabel("Left Hand", AlignVertical.Top));
        bottomRight.Children.Add(CreateHudLabel("Sword", AlignVertical.Bottom));
        layout.SetCell(2, 2, bottomRight);

        Children.Add(viewLayout);
        Children.Add(layout);
        var uiView = new View
        {
            Camera = MainCamera,
            LayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            PreserveDrawOrder = true,
        };
        // UI uses painter's order: labels and icons must overlay their panel backgrounds.
        uiView.ViewComponent.EnableDepthTest = false;
        uiView.ViewComponent.EnableDepthWrite = false;
        Children.Add(uiView);
        Children.Add(_background);
        Children.Add(_formation);
        ApplyWorldSize();
    }

    private ImageElement CreateHudImage(string contentId, Rectangle<int> sourceRegion) => new()
    {
        Texture = _textures.GetOrCreate(new ContentId(contentId)),
        SourceRegion = sourceRegion,
        Width = 210f,
        Height = 180f,
        Margins = new(0f, 0f, 24f, 24f),
        HorizontalAlignment = AlignHorizontal.Center,
        VerticalAlignment = AlignVertical.Center,
        SizingMode = ImageSizingMode.Fit,
        SortOrder = 1,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    private TextElement CreateHudLabel(string text, AlignVertical alignment) => new(text, _hudTextStyle)
    {
        Height = 20f,
        HorizontalAlignment = AlignHorizontal.Center,
        VerticalAlignment = alignment,
        Margins = new(10f, 10f, 8f, 8f),
        SortOrder = 2,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };

    /// <summary>Uses a named region of the shared atlas as a cell's panel artwork.</summary>
    private static AtlasPanel CreateAtlasPanel(ITexture atlas, string regionName) =>
        new(atlas, regionName,
            regionName is "region-0000" or "region-0001"
                ? new(32f, 32f, 32f, 32f)
                : new(48f, 32f, 48f, 32f))
        {
            Margins = new(3f, 3f),
        };

    /// <inheritdoc />
    public override void Update(double deltaTime)
    {
        base.Update(deltaTime);

        ApplyWorldSize();
    }

    /// <summary>Updates the fixed-size world camera and world-space content.</summary>
    private void ApplyWorldSize()
    {
        _worldCamera.SetSize(_worldSize.X, _worldSize.Y);
        _worldCamera.Position = new(_worldSize.X * 0.5f, _worldSize.Y * 0.5f, 1f);
        _background.SetWorldSize(_worldSize);
        _formation.SetWorldSize(_worldSize);
    }
}
