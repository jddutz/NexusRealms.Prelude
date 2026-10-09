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
    private readonly IGraphicalUserInterface _gui;
    private AtlasPanel? _leftHandPanel;
    private AtlasPanel? _rightHandPanel;

    /// <summary>The hand whose action is currently selected for the player's turn.</summary>
    public PlayerHand ActiveHand { get; private set; } = PlayerHand.Left;

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
        GameState gameState,
        IGraphicalUserInterface gui
    )
    {
        _textures = textures;
        _storyline = storyline;
        _windowService = windowService;
        _gameState = gameState;
        _gui = gui;
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
        var uiAtlas = _textures.GetOrCreate(new ContentId("ui.ui_panels.png"));
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

        var atlas = _textures.GetOrCreate(new ContentId("ui.ui_panels.png"));
        layout.SetCell(0, 2, _retreatButton);
        var turnOrder = new TurnOrderStrip(atlas, _hudTextStyle)
        {
            Width = (1 + _startScenario.Characters.Length) * 80f + _startScenario.Characters.Length * 10f,
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
                Width = 10f,
                Height = 10f,
                VerticalAlignment = AlignVertical.Top,
                Margins = new(0f, 0f, 33f, 0f),
                SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
            turnOrder.Items.Children.Add(new TurnOrderPortrait(
                _textures.GetOrCreate(_storyline.Characters[placement.CharacterId].Portrait), atlas));
        }
        layout.SetCell(0, 1, turnOrder);
        var bottomLeft = CreateAtlasPanel(atlas, "region-0000");
        _leftHandPanel = bottomLeft;
        bottomLeft.IsSelected = ActiveHand == PlayerHand.Left;
        // A primary-pointer press and release within the panel simulates a tap.
        bottomLeft.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
            .Invoke(() => SelectHand(PlayerHand.Left));
        bottomLeft.InputMap.OnLongPress(() => OpenHandDialog(PlayerHand.Left));
        bottomLeft.Margins = new(10f, 10f);
        // The ice spell occupies the second cell in the sheet's first row.
        bottomLeft.Children.Add(CreateHudImage("abilities.basic_spells.png", new(410, 0, 397, 334)));
        bottomLeft.Children.Add(CreateHudLabel("Left Hand", AlignVertical.Top));
        bottomLeft.Children.Add(CreateHudLabel("Frost", AlignVertical.Bottom));
        layout.SetCell(2, 0, bottomLeft);
        var bottomCenter = CreateAtlasPanel(atlas, "region-0006");
        bottomCenter.Height = 128f;
        bottomCenter.Margins = new(3f, 3f, 3f, 10f);
        bottomCenter.VerticalAlignment = AlignVertical.Bottom;
        bottomCenter.Children.Add(new PlayerHudPortrait(_textures.GetOrCreate(_gameState.Portrait)));
        bottomCenter.Children.Add(new PlayerStatusBars(_gameState,
            _textures.GetOrCreate(new ContentId("icons.status_icons.png"))));
        bottomCenter.Children.Add(CreateZodiacImage());
        layout.SetCell(2, 1, bottomCenter);
        var bottomRight = CreateAtlasPanel(atlas, "region-0000");
        _rightHandPanel = bottomRight;
        bottomRight.IsSelected = ActiveHand == PlayerHand.Right;
        bottomRight.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
            .Invoke(() => SelectHand(PlayerHand.Right));
        bottomRight.InputMap.OnLongPress(() => OpenHandDialog(PlayerHand.Right));
        bottomRight.Margins = new(10f, 10f);
        // The sword occupies the first cell in the sheet's first row.
        bottomRight.Children.Add(CreateHudImage("equipment.weapons_one_handed.png", new(0, 0, 405, 334)));
        bottomRight.Children.Add(CreateHudLabel("Right Hand", AlignVertical.Top));
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

    private void OpenHandDialog(PlayerHand hand)
    {
        var dialog = _gui.StartModalDialog();
        dialog.Content.Height = 340f;
        var atlas = _textures.GetOrCreate(new ContentId("ui.ui_panels.png"));
        var panel = CreateAtlasPanel(atlas, "region-0006");
        dialog.Content.Children.Add(panel);
        var left = hand == PlayerHand.Left;
        panel.Children.Add(new TextElement(left ? "Left Hand — Frost" : "Right Hand — Sword", _hudTextStyle)
        {
            Height = 32f,
            VerticalAlignment = AlignVertical.Top,
            Margins = new(24f, 24f),
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        });
        panel.Children.Add(CreateHudImage(
            left ? "abilities.basic_spells.png" : "equipment.weapons_one_handed.png",
            left ? new(410, 0, 397, 334) : new(0, 0, 405, 334)));
        panel.Children.Add(new TextButton
        {
            Label = "Close",
            Style = _hudTextStyle,
            Texture = atlas,
            TexCoord = _retreatButton.TexCoord,
            SourceBorders = new(48f, 32f, 48f, 32f),
            BorderScale = 0.4f,
            Width = 120f,
            Height = 44f,
            VerticalAlignment = AlignVertical.Bottom,
            Margins = new(16f, 16f),
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            Action = _ => dialog.Dispose(),
        });
    }

    private void SelectHand(PlayerHand hand)
    {
        ActiveHand = hand;
        if (_leftHandPanel is not null)
            _leftHandPanel.IsSelected = hand == PlayerHand.Left;
        if (_rightHandPanel is not null)
            _rightHandPanel.IsSelected = hand == PlayerHand.Right;
    }

    private ImageElement CreateZodiacImage()
    {
        // Authored bounds for the twelve signs, in zodiac order. This sheet is
        // irregularly spaced, so equal-sized grid cells would cut off the artwork.
        Rectangle<int>[] signs =
        [
            new(0, 0, 375, 315),
            new(420, 0, 380, 315),
            new(803, 0, 393, 340),
            new(1200, 0, 364, 315),
            new(0, 323, 425, 334),
            new(445, 323, 366, 369),
            new(830, 323, 360, 344),
            new(1200, 323, 364, 344),
            new(0, 638, 435, 368),
            new(435, 661, 381, 345),
            new(825, 662, 377, 344),
            new(1202, 666, 362, 340),
        ];
        return new ImageElement
        {
            Texture = _textures.GetOrCreate(new ContentId("zodiac_signs")),
            SourceRegion = signs[Random.Shared.Next(signs.Length)],
            Width = 112f,
            Height = 112f,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Center,
            Margins = new(0f, 16f, 0f, 0f),
            SizingMode = ImageSizingMode.Fit,
            SortOrder = 1,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
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
