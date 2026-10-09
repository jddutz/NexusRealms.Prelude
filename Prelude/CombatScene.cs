namespace NexusRealms.Prelude;

/// <summary>
/// Builds the opening combat encounter declared by the storyline.
/// </summary>
public class CombatScene : Scene
{
    private const float SideHudWidth = 144f;
    private const float TopHudHeight = 120f;
    private const float BottomHudHeight = 134f;
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
    public Combat.CombatSystem Combat { get; }
    public int EncounterSeed { get; }
    public TimelineEventRenderer TimelineRenderer { get; }
    private View? _worldView;
    private PanelElement? _centerHudPanel;
    private TurnOrderStrip? _turnOrder;
    private readonly List<(Character Character, CharacterStatusDisplay Display)> _characterDisplays = [];
    private Character? _pointerTarget;
    private Character? _pressedTarget;
    public Character? FocusedCharacter { get; private set; }
    public event Action<Character?>? FocusChanged;
    private readonly Dictionary<string, ContentId> _turnPortraits = [];
    private PanelElement? _leftHandPanel;
    private PanelElement? _rightHandPanel;

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
        TimelineRenderer = new(CreateTimelineElement);
        TimelineRenderer.Changed += RefreshTurnOrder;

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

        EncounterSeed = _startScenario.RandomSeed ?? Random.Shared.Next();
        Combat = new(unchecked((uint)EncounterSeed));
        var startingStats = new Random(EncounterSeed);

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
            Action = _ => Combat.End(),
            Width = 120f,
            Height = 54f,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Top,
            Style = textStyles.GetOrCreate(BuiltInFonts.Default, 16f),
            Texture = uiAtlas,
            TexCoord = new(
                retreatRegion.Origin.X,
                retreatRegion.Origin.Y,
                retreatRegion.Size.X,
                retreatRegion.Size.Y
            ),
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
            if (placement.Team == NexusRealms.Prelude.Combat.CombatTeam.Enemies)
                character = character with
                {
                    Health = startingStats.Next(3, 8),
                    Focus = startingStats.Next(0, 4),
                    Initiative = startingStats.Next(0, 6),
                };
            var combatCharacter = new Character(
                character,
                _textures.GetOrCreate(character.Artwork),
                _textures.GetOrCreate(new ContentId("shadow"))
            );
            combatCharacter.Renderer.RenderLayerMask = _worldLayerMask;
            combatCharacter.ShadowRenderer.RenderLayerMask = _worldLayerMask;
            combatCharacter.ConfigureFocusIndicator(
                _textures.GetOrCreate(new ContentId("ui.selection_indicator.png")),
                _textures.GetOrCreate(new ContentId("ui.invalid_selection.png")), _worldLayerMask);
            _formation.SetSlot(placement.Slot, combatCharacter);
            var display = new CharacterStatusDisplay(combatCharacter)
            {
                Width = 132f, Height = 44f, SortOrder = 10, IsVisible = false,
            };
            _characterDisplays.Add((combatCharacter, display));
        }

        _turnPortraits.Add("player", _gameState.Portrait);
        Combat.Add(new Combat.Combatant("player", true, initiative: _gameState.Initiative));
        for (var index = 0; index < _startScenario.Characters.Length; index++)
        {
            var placement = _startScenario.Characters[index];
            var id = $"encounter-{index}";
            _turnPortraits.Add(id, _storyline.Characters[placement.CharacterId].Portrait);
            var row = placement.Slot switch
            {
                <= FormationSlot.BackRight => NexusRealms.Prelude.Combat.CombatRow.Back,
                <= FormationSlot.MiddleRight => NexusRealms.Prelude.Combat.CombatRow.Middle,
                _ => NexusRealms.Prelude.Combat.CombatRow.Front,
            };
            Combat.Add(
                new Combat.Combatant(
                    id,
                    false,
                    placement.InitialTurn,
                    _formation[placement.Slot]!.Definition.Initiative,
                    placement.Team,
                    row
                )
            );
        }
        // Until authored abilities/AI are available, opponents pass with a standard cost.
        Combat.DecideAction = (_, _) => new Combat.CombatAction(0.6f, (_, _) => { });
        foreach (var occurrence in _startScenario.Events)
            Combat.ScheduleEvent(
                occurrence.Turn,
                occurrence.Label,
                occurrence.Resolve,
                occurrence.Priority,
                occurrence.Icon,
                occurrence.IsVisible,
                occurrence.PresentationKey
            );
        Combat.Changed += RefreshTurnOrder;
        Combat.Changed += () => { if (Combat.HasEnded) FocusCharacter(null); };
        Combat.Process();

        var inputMap = new InputMap(eventHub, HitTestTargetSelection);
        inputMap.OnMouseButtonPressed(MouseButtonEnum.Left).Invoke(() => _pressedTarget = _pointerTarget);
        inputMap.OnMouseButtonReleased(MouseButtonEnum.Left).Invoke(() =>
        {
            if (_pressedTarget == _pointerTarget) FocusCharacter(_pointerTarget);
        });
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
        _worldView = worldView;
        worldView.SortOrder = -100;
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
        var turnOrder = new TurnOrderStrip(atlas, _hudTextStyle,
            _textures.GetOrCreate(new ContentId("shadow")),
            _textures.GetOrCreate(new ContentId("square_shadow")))
        {
            Width =
                (1 + _startScenario.Characters.Length) * 80f
                + _startScenario.Characters.Length * 10f,
            Height = 96f,
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Top,
            Margins = new(0f),
        };
        _turnOrder = turnOrder;
        RefreshTurnOrder();
        Children.Add(turnOrder);
        var bottomLeft = CreatePanelElement(atlas, "region-0000");
        _leftHandPanel = bottomLeft;
        bottomLeft.IsSelected = ActiveHand == PlayerHand.Left;
        // A primary-pointer press and release within the panel simulates a tap.
        bottomLeft
            .InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
            .Invoke(() => SelectHand(PlayerHand.Left));
        bottomLeft.InputMap.OnLongPress(() => OpenHandDialog(PlayerHand.Left));
        bottomLeft.Margins = new(10f, 10f);
        // The ice spell occupies the second cell in the sheet's first row.
        bottomLeft.Children.Add(
            CreateHudImage("abilities.basic_spells.png", new(410, 0, 397, 334))
        );
        bottomLeft.Children.Add(CreateHudLabel("Left Hand", AlignVertical.Top));
        bottomLeft.Children.Add(CreateHudLabel("Frost", AlignVertical.Bottom));
        layout.SetCell(2, 0, bottomLeft);
        var bottomCenter = CreatePanelElement(atlas, "region-0006");
        _centerHudPanel = bottomCenter;
        bottomCenter.Height = 84f;
        bottomCenter.Margins = new(3f, 3f, 3f, 10f);
        bottomCenter.VerticalAlignment = AlignVertical.Bottom;
        bottomCenter.Children.Add(
            new PlayerHudPortrait(_textures.GetOrCreate(_gameState.Portrait))
        );
        bottomCenter.Children.Add(
            new PlayerStatusBars(
                _gameState,
                _textures.GetOrCreate(new ContentId("icons.status_icons.png"))
            )
        );
        bottomCenter.Children.Add(CreateZodiacImage());
        layout.SetCell(2, 1, bottomCenter);
        var bottomRight = CreatePanelElement(atlas, "region-0000");
        _rightHandPanel = bottomRight;
        bottomRight.IsSelected = ActiveHand == PlayerHand.Right;
        bottomRight
            .InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
            .Invoke(() => SelectHand(PlayerHand.Right));
        bottomRight.InputMap.OnLongPress(() => OpenHandDialog(PlayerHand.Right));
        bottomRight.Margins = new(10f, 10f);
        // The sword occupies the first cell in the sheet's first row.
        bottomRight.Children.Add(
            CreateHudImage("equipment.weapons_one_handed.png", new(0, 0, 405, 334))
        );
        bottomRight.Children.Add(CreateHudLabel("Right Hand", AlignVertical.Top));
        bottomRight.Children.Add(CreateHudLabel("Sword", AlignVertical.Bottom));
        layout.SetCell(2, 2, bottomRight);

        Children.Add(worldView);
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
        foreach (var (_, display) in _characterDisplays) Children.Add(display);
        ApplyWorldSize();
    }

    /// <summary>Called by the action selection flow once its effects and cost are chosen.</summary>
    public void SubmitAction(Combat.CombatAction action) => Combat.SubmitAction(action);

    /// <summary>Changes selection without resolving an action, including invalid targets.</summary>
    public void FocusCharacter(Character? character)
    {
        if (Combat.HasEnded) character = null;
        FormationSlot? selectedSlot = null;
        foreach (var slot in Enum.GetValues<FormationSlot>())
            if (character is not null && _formation[slot] == character) selectedSlot = slot;
        if (character is not null && selectedSlot is null)
            throw new ArgumentException("Character is not in this encounter.", nameof(character));
        if (FocusedCharacter == character) return;
        var previous = FocusedCharacter;
        FocusedCharacter = character;
        previous?.SetFocus(false, previous.IsValidTarget);
        character?.SetFocus(true, selectedSlot >= FormationSlot.FrontLeft);
        foreach (var (owner, display) in _characterDisplays)
            display.IsVisible = owner == character && !Combat.HasEnded;
        FocusChanged?.Invoke(character);
    }

    private bool HitTestTargetSelection(Vector2D<float> screenPosition)
    {
        _pointerTarget = null;
        if (Combat.HasEnded || _worldView is null) return false;
        bool Contains(Rectangle<float> rect) => screenPosition.X >= rect.Origin.X
            && screenPosition.Y >= rect.Origin.Y
            && screenPosition.X < rect.Origin.X + rect.Size.X
            && screenPosition.Y < rect.Origin.Y + rect.Size.Y;
        if (!Contains(_worldView.Bounds)
            || (_centerHudPanel is { } center && Contains(center.Bounds))
            || (_leftHandPanel is { } left && Contains(left.Bounds))
            || (_rightHandPanel is { } right && Contains(right.Bounds))
            || Contains(_retreatButton.Bounds)
            || (_turnOrder is { } order && screenPosition.Y < order.Bounds.Origin.Y + 130f)) return false;
        var viewport = _worldView.ViewComponent.ViewportRegion;
        if (viewport.Size.X <= 0 || viewport.Size.Y <= 0) return false;
        var world = new Vector2D<float>(
            (screenPosition.X - viewport.Origin.X) * _worldSize.X / viewport.Size.X,
            (screenPosition.Y - viewport.Origin.Y) * _worldSize.Y / viewport.Size.Y);
        // Hit the frontmost drawn opaque character when sprites overlap.
        foreach (var slot in Enum.GetValues<FormationSlot>().Reverse())
            if (_formation[slot] is { } candidate && candidate.HitTest(world))
            {
                _pointerTarget = candidate;
                break;
            }
        return true;
    }
    private void RefreshTurnOrder()
    {
        if (_turnOrder is null)
            return;
        var entries = new List<(float Turn, Element Element)>();
        if (Combat.ActiveCombatant is { } active)
        {
            var occurrence = new Combat.TimelineOccurrence(
                0,
                Combat.Timeline.CurrentTurn,
                NexusRealms.Prelude.Combat.TimelinePriority.Combatant,
                0,
                active.Id,
                active.Id
            );
            entries.Add((occurrence.Turn, TimelineRenderer.Create(occurrence, isActive: true)));
        }
        var eventCount = 0;
        foreach (var occurrence in Combat.Timeline.TurnOrder.Where(x => x.IsVisible))
        {
            if (occurrence.CombatantId is null && eventCount++ >= 7)
                continue;
            entries.Add((occurrence.Turn, TimelineRenderer.Create(occurrence)));
        }
        _turnOrder.SetOccurrences(Combat.Timeline.CurrentTurn, entries);
        _turnOrder.Width = 640f;
    }

    private Element CreateTimelineElement(Combat.TimelineOccurrence occurrence, bool isActive)
    {
        if (occurrence.CombatantId is { } id)
            return new TurnOrderPortrait(
                _textures.GetOrCreate(_turnPortraits[id]),
                _textures.GetOrCreate(new ContentId("ui.ui_panels.png"))
            )
            {
                IsActive = isActive,
            };
        if (occurrence.Icon is { } icon)
            return new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId(icon)),
                Width = 64f,
                Height = 64f,
                SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
        return new TextElement(occurrence.Label, _hudTextStyle)
        {
            Width = 80f,
            Height = 64f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
    }

    private void OpenHandDialog(PlayerHand hand)
    {
        var dialog = _gui.StartModalDialog();
        dialog.Content.Height = 340f;
        var atlas = _textures.GetOrCreate(new ContentId("ui.ui_panels.png"));
        var panel = CreatePanelElement(atlas, "region-0006");
        dialog.Content.Children.Add(panel);
        var left = hand == PlayerHand.Left;
        panel.Children.Add(
            new TextElement(left ? "Left Hand — Frost" : "Right Hand — Sword", _hudTextStyle)
            {
                Height = 32f,
                VerticalAlignment = AlignVertical.Top,
                Margins = new(24f, 24f),
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            }
        );
        panel.Children.Add(
            CreateHudImage(
                left ? "abilities.basic_spells.png" : "equipment.weapons_one_handed.png",
                left ? new(410, 0, 397, 334) : new(0, 0, 405, 334), scale: 1f
            )
        );
        panel.Children.Add(
            new TextButton
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
            }
        );
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
            Width = 60f,
            Height = 60f,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Center,
            Margins = new(0f, 8f, 0f, 0f),
            SizingMode = ImageSizingMode.Fit,
            SortOrder = 1,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
    }

    private ImageElement CreateHudImage(string contentId, Rectangle<int> sourceRegion, float scale = 0.6f) =>
        new()
        {
            Texture = _textures.GetOrCreate(new ContentId(contentId)),
            SourceRegion = sourceRegion,
            Width = 210f * scale,
            Height = 180f * scale,
            Margins = new(0f, 0f, 24f * scale, 24f * scale),
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Center,
            SizingMode = ImageSizingMode.Fit,
            SortOrder = 1,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };

    private TextElement CreateHudLabel(string text, AlignVertical alignment) =>
        new(text, _hudTextStyle)
        {
            Height = 20f,
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = alignment,
            Margins = new(10f, 10f, 8f, 8f),
            SortOrder = 2,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };

    /// <summary>Uses a named region of the shared atlas as a cell's panel artwork.</summary>
    private static PanelElement CreatePanelElement(ITexture atlas, string regionName) =>
        new(
            atlas,
            regionName,
            regionName is "region-0000" or "region-0001"
                ? new(32f, 32f, 32f, 32f)
                : new(48f, 32f, 48f, 32f)
        )
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
        if (_worldView is { } view && _centerHudPanel is { } hud && hud.Bounds.Size.Y > 0f)
        {
            var viewport = view.ViewComponent.ViewportRegion;
            if (viewport.Size.Y > 0)
            {
                // Map actual screen layout back through the Fill viewport, including cropping.
                var worldPerPixel = _worldSize.Y / viewport.Size.Y;
                var top =
                    (view.Bounds.Origin.Y + TopHudHeight + 24f - viewport.Origin.Y) * worldPerPixel;
                var bottom = (hud.Bounds.Origin.Y - 32f - viewport.Origin.Y) * worldPerPixel;
                _formation.SetVerticalLimits(top, bottom);
                foreach (var (character, display) in _characterDisplays)
                {
                    display.IsVisible = character.IsFocused && !Combat.HasEnded;
                    var pixelsPerWorldX = viewport.Size.X / _worldSize.X;
                    var pixelsPerWorldY = viewport.Size.Y / _worldSize.Y;
                    character.SetIndicatorScreenScale(1f / pixelsPerWorldX, 1f / pixelsPerWorldY);
                    var headX = viewport.Origin.X + character.Position.X * pixelsPerWorldX;
                    var headY = viewport.Origin.Y + (character.Position.Y
                        - (character.Texture.Height + character.Elevation) * character.Scale.Y) * pixelsPerWorldY;
                    var symbolWidth = character.FocusRenderer.Instances.Values.First().Size.X
                        * character.Scale.X * pixelsPerWorldX;
                    var symbolHeight = character.FocusRenderer.Instances.Values.First().Size.Y
                        * character.Scale.Y * pixelsPerWorldY;
                    display.Arrange(new(headX + symbolWidth * 0.5f + 8f,
                        headY - 8f - symbolHeight * 0.5f - 22f, 132f, 44f));
                }
            }
        }
    }
}








