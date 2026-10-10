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
    private PlayerHudPortrait? _playerPortrait;
    private PanelElement? _confirmationPanel;

    /// <summary>The player's committed loadout and free quick-slot customization.</summary>
    public Combat.CombatLoadout Loadout => _gameState.Loadout;
    public string? SelectedActionId { get; private set; }
    private int? _selectedQuickSlot;
    private ModalDialog? _dialog;
    private bool _closeDialogRequested;
    private string? _inspectedItem;
    private int _inventoryPage;
    private AbilityId? _selectedZodiacAbility;
    private Element? _assignmentChoices;
    private Dictionary<string, string>? _pending;
    private ImageElement? _actionIcon;
    private string? _renderedIconKey;
    private TextElement? _actionLabel;
    private readonly List<ImageElement> _quickSlots = [];
    private readonly List<ImageElement> _quickSlotIcons = [];
    private Combat.CommandTarget? CurrentTarget()
    {
        var index = Array.FindIndex(_startScenario.Characters.ToArray(), p => _formation[p.Slot] == FocusedCharacter);
        if (index < 0) return null;
        var placement = _startScenario.Characters[index];
        var row = placement.Slot >= FormationSlot.FrontLeft ? NexusRealms.Prelude.Combat.CombatRow.Front
            : placement.Slot >= FormationSlot.MiddleLeft ? NexusRealms.Prelude.Combat.CombatRow.Middle : NexusRealms.Prelude.Combat.CombatRow.Back;
        return new(Combat.Contains($"encounter-{index}"), FocusedCharacter!.Health > 0, placement.Team, row);
    }
    private bool ValidTarget() => Loadout.ValidTarget(Loadout.Find(SelectedActionId), CurrentTarget());
    public void SelectQuickAction(int slot)
    {
        if (_dialog is not null || (uint)slot >= Loadout.QuickSlots.Length) return;
        if (!Loadout.CanSelectQuickSlot(slot)) return;
        _selectedQuickSlot = slot;
        SelectedActionId = Loadout.QuickSlots[slot];
        RefreshCommand();
    }
    public bool ConfirmCombatAction()
    {
        var target = FocusedCharacter;
        return _dialog is null && Loadout.Confirm(Combat, "player", Loadout.Find(SelectedActionId), CurrentTarget, () => { }, strike =>
        {
            if (target is null) return;
            target.Health = Math.Max(0, target.Health - strike.Damage);
            if (target.Health != 0) return;
            var index = Array.FindIndex(_startScenario.Characters.ToArray(), p => _formation[p.Slot] == target);
            if (index >= 0) Combat.Remove($"encounter-{index}");
            target.Renderer.IsVisible = false;
            target.ShadowRenderer.IsVisible = false;
            FocusCharacter(null);
        });
    }
    private void RefreshCommand()
    {
        if (_selectedQuickSlot is { } selectedSlot
            && (!Loadout.CanSelectQuickSlot(selectedSlot) || Loadout.QuickSlots[selectedSlot] != SelectedActionId))
        {
            _selectedQuickSlot = null;
            SelectedActionId = null;
        }
        var a = Loadout.Find(SelectedActionId);
        var actionTexture = a is null ? null : Loadout.CommandIcon(a);
        var valid = Loadout.CanConfirm(Combat, "player", a, ValidTarget());
        if (_actionIcon is not null)
        {
            _actionIcon.IsVisible = actionTexture is not null;
            _actionIcon.Color = new Color(1f, 1f, 1f, valid ? 1f : 0.4f);
            if (a is not null && actionTexture is { } icon && _renderedIconKey != $"{icon}:{a.IconRegion}")
            {
                _renderedIconKey = $"{icon}:{a.IconRegion}";
                _actionIcon.SourceRegion = null;
                _actionIcon.Texture = _textures.GetOrCreate(new ContentId(icon));
                if (a.IconRegion is { } region) _actionIcon.SourceRegion = _actionIcon.Texture.GetRegion(region).Bounds;
            }
        }
        if (_actionLabel is not null) _actionLabel.Text = a?.Name ?? "Select action";
        for (var i = 0; i < _quickSlots.Count; i++)
        {
            var q = Loadout.Find(Loadout.QuickSlots[i]);
            _quickSlots[i].Texture = _textures.GetOrCreate(new ContentId(
                q is not null && _selectedQuickSlot == i && q.Id.Value == SelectedActionId ? "ui.item_frame_selected.png" : "ui.item_frame.png"));
            var slotIcon = _quickSlotIcons[i];
            var quickTexture = q is null ? null : Loadout.CommandIcon(q);
            slotIcon.IsVisible = quickTexture is not null;
            slotIcon.Color = new Color(1f, 1f, 1f, q is not null && Loadout.Available(q) ? 1f : 0.4f);
            slotIcon.SourceRegion = null;
            if (q is not null && quickTexture is { } slotTexture)
            {
                slotIcon.Texture = _textures.GetOrCreate(new ContentId(slotTexture));
                if (q.IconRegion is { } region) slotIcon.SourceRegion = slotIcon.Texture.GetRegion(region).Bounds;
            }
        }
        FocusedCharacter?.SetFocus(true, a is null || !a.RequiresTarget || ValidTarget());
        foreach (var (character, display) in _characterDisplays)
            display.Eligibility = a is null || !a.RequiresTarget ? "" : ValidTarget() ? "Valid target" : "Invalid target";
    }

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
        var buttonTexture = _textures.GetOrCreate(new ContentId("ui.panel_small.png"));
        _retreatButton = new TextButton
        {
            Label = "Retreat",
            TextColor = UiTheme.TextColor,
            Action = _ => Combat.End(),
            Width = 120f,
            Height = 54f,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Top,
            Style = textStyles.GetOrCreate(BuiltInFonts.Default, 16f),
            Texture = buttonTexture,
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
            var display = new CharacterStatusDisplay(combatCharacter, _textures, _hudTextStyle)
            {
                Width = 132f,
                Height = 66f,
                SortOrder = 10,
                IsVisible = false,
            };
            _characterDisplays.Add((combatCharacter, display));
        }

        _turnPortraits.Add("player", _gameState.Portrait);
        Combat.Add(new Combat.Combatant("player", true, initiative: _startScenario.PlayerInitiative));
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
                    placement.Initiative,
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

        layout.SetCell(0, 2, _retreatButton);
        var turnOrder = new TurnOrderStrip(_textures, _hudTextStyle,
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
        var bottomLeft = new PlayerHudPortrait(_textures.GetOrCreate(_gameState.Portrait));
        _playerPortrait = bottomLeft;
        bottomLeft.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left).Invoke(OpenCharacterDialog);
        layout.SetCell(2, 0, bottomLeft);
        var bottomCenter = CreatePanelElement("ui.panel_wide.png");
        _centerHudPanel = bottomCenter;
        bottomCenter.Height = 84f;
        bottomCenter.Margins = new(3f, 3f, 3f, 10f);
        bottomCenter.VerticalAlignment = AlignVertical.Bottom;
        const float actionSlotSize = 56f;
        const float actionSlotGap = 4f;
        var actionSlots = new GridLayout
        {
            Width = Loadout.QuickSlots.Length * (actionSlotSize + actionSlotGap) - actionSlotGap,
            Height = actionSlotSize,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Center,
            Margins = new(Left: 0f, Right: 100f, Top: 0f, Bottom: 0f),
            Columns = Enumerable.Repeat(GridSize.Absolute(actionSlotSize + actionSlotGap), Loadout.QuickSlots.Length - 1)
                .Append(GridSize.Absolute(actionSlotSize)).ToArray(),
            Rows = [GridSize.Absolute(actionSlotSize)],
        };
        for (var i = 0; i < Loadout.QuickSlots.Length; i++)
        {
            var slot = i;
            var frame = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("ui.item_frame.png")),
                Width = actionSlotSize, Height = actionSlotSize,
                HorizontalAlignment = AlignHorizontal.Left,
                SizingMode = ImageSizingMode.Stretch,
                SortOrder = 2,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            frame.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left).Invoke(() => SelectQuickAction(slot));
            var icon = new ImageElement
            {
                Width = 36f, Height = 36f, SizingMode = ImageSizingMode.Fit,
                IsVisible = false,
                SortOrder = 3,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            frame.Children.Add(icon);
            actionSlots.SetCell(0, i, frame);
            _quickSlots.Add(frame);
            _quickSlotIcons.Add(icon);
        }
        bottomCenter.Children.Add(actionSlots);
        bottomCenter.Children.Add(
            new PlayerStatusBars(
                _gameState,
                _textures
            )
        );
        bottomCenter.Children.Add(CreateZodiacImage());
        layout.SetCell(2, 1, bottomCenter);
        var bottomRight = CreatePanelElement("ui.panel_square.png", square: true);
        _confirmationPanel = bottomRight;
        bottomRight.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left).Invoke(() => ConfirmCombatAction());
        _actionIcon = CreateHudImage("equipment.weapons_one_handed.png", new(0, 0, 405, 334), 0.3f);
        bottomRight.Children.Add(_actionIcon);
        bottomRight.Children.Add(CreateHudLabel("Confirm", AlignVertical.Top));
        _actionLabel = CreateHudLabel("Select action", AlignVertical.Bottom);
        bottomRight.Children.Add(_actionLabel);
        RefreshCommand();
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
        character?.SetFocus(true, Loadout.Find(SelectedActionId) is not { RequiresTarget: true } || ValidTarget());
        RefreshCommand();
        foreach (var (owner, display) in _characterDisplays)
            display.IsVisible = owner == character && !Combat.HasEnded;
        FocusChanged?.Invoke(character);
    }

    private bool HitTestTargetSelection(Vector2D<float> screenPosition)
    {
        _pointerTarget = null;
        if (_dialog is not null || Combat.HasEnded || _worldView is null) return false;
        bool Contains(Rectangle<float> rect) => screenPosition.X >= rect.Origin.X
            && screenPosition.Y >= rect.Origin.Y
            && screenPosition.X < rect.Origin.X + rect.Size.X
            && screenPosition.Y < rect.Origin.Y + rect.Size.Y;
        if (!Contains(_worldView.Bounds)
            || (_centerHudPanel is { } center && Contains(center.Bounds))
            || (_playerPortrait is { } left && Contains(left.Bounds))
            || (_confirmationPanel is { } right && Contains(right.Bounds))
            || Contains(_retreatButton.Bounds)
            || (_turnOrder is { } order && screenPosition.Y < order.Bounds.Origin.Y + 130f)) return false;
        var viewport = _worldView.ViewComponent.ViewportRegion;
        if (viewport.Size.X <= 0 || viewport.Size.Y <= 0) return false;
        var world = new Vector2D<float>(
            (screenPosition.X - viewport.Origin.X) * _worldSize.X / viewport.Size.X,
            (screenPosition.Y - viewport.Origin.Y) * _worldSize.Y / viewport.Size.Y);
        // Hit the frontmost drawn opaque character when sprites overlap.
        foreach (var slot in Enum.GetValues<FormationSlot>().Reverse())
            if (_formation[slot] is { } candidate && candidate.Health > 0 && candidate.HitTest(world))
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
                _textures,
                isEnemy: id != "player" && _startScenario.Characters[int.Parse(id["encounter-".Length..])].Team == NexusRealms.Prelude.Combat.CombatTeam.Enemies
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
            Color = UiTheme.TextColor,
            Width = 80f,
            Height = 64f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
    }

    private TextButton DialogButton(string label, Action action) => new()
    {
        Label = label,
        TextColor = UiTheme.TextColor,
        Style = _hudTextStyle,
        Width = 190f,
        Height = 36f,
        Padding = new(8f, 4f),
        Texture = _textures.GetOrCreate(new ContentId("ui.panel_small.png")),
        SourceBorders = new(48f, 32f, 48f, 32f),
        BorderScale = 0.4f,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        Action = _ => action(),
    };
    private void Place(Element panel, Element control, float x, float y)
    {
        control.HorizontalAlignment = AlignHorizontal.Left; control.VerticalAlignment = AlignVertical.Top;
        control.Margins = new(Left: x, Right: 0f, Top: y, Bottom: 0f); panel.Children.Add(control);
    }
    private void CloseDialog()
    {
        // Keep the scope alive through the current event dispatch, including Escape/Back.
        _pending = null; _closeDialogRequested = true;
    }
    private PanelElement BeginDialog(string? title)
    {
        _dialog = _gui.StartModalDialog(); _dialog.Content.Width = 900f; _dialog.Content.Height = 460f;
        _dialog.InputMap.OnKeyPressed(KeyEnum.Escape).Invoke(CloseDialog);
        _dialog.InputMap.OnAnyControllerButtonPressed(ControllerSemanticNames.Back).Invoke(CloseDialog);
        var panel = CreatePanelElement("ui.panel_large.png");
        _dialog.Content.Children.Add(panel);
        if (title is not null)
        {
            panel.Children.Add(CreateHudLabel(title, AlignVertical.Top));
        }
        return panel;
    }
    private void PlaceDialogFooterButton(Element panel, string label, Action action, float x, float y)
    {
        var button = DialogButton(label, action);
        button.Width = 110f;
        Place(panel, button, x, y);
    }
    private void AssignControls(Element panel, Combat.CommandDefinition a, float y, float destinationY = 350f)
    {
        Place(panel, DialogButton($"Assign {a.Name}", () =>
        {
            if (_assignmentChoices is not null) panel.Children.Remove(_assignmentChoices);
            _assignmentChoices = new Element { Height = 36f, VerticalAlignment = AlignVertical.Top, Margins = new(Left: 0f, Right: 0f, Top: destinationY, Bottom: 0f) };
            panel.Children.Add(_assignmentChoices);
            for (var i = 0; i < Loadout.QuickSlots.Length; i++)
            {
                var slot = i;
                var button = DialogButton($"Slot {i + 1}", () => { Loadout.Assign(slot, a.Id.Value); RefreshCommand(); });
                button.Width = 80f;
                Place(_assignmentChoices, button, 24f + i * 86f, 0f);
            }
        }), 24f, y);
    }
    public void OpenCharacterDialog()
    {
        if (_dialog is not null) return; _pending = Loadout.BeginEquipment(); RenderCharacterDialog();
    }
    private const float ItemCellSize = 56f;
    private const float InventoryCellGap = 4f;
    private Element ItemCell(Combat.CarriedItem? item, string? placeholder, Action action,
        IReadOnlyDictionary<string, Element>? equipmentTargets = null, string? handSlot = null)
    {
        // Scale the complete frame uniformly as a single image, rather than slicing its corners.
        var frame = new ImageElement
        {
            Texture = _textures.GetOrCreate(new ContentId("ui.item_frame.png")),
            Width = ItemCellSize, Height = ItemCellSize,
            SizingMode = ImageSizingMode.Stretch,
            HorizontalAlignment = AlignHorizontal.Left,
            VerticalAlignment = AlignVertical.Top,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        var handAction = NexusRealms.Prelude.Combat.CombatLoadout.HandAction(handSlot);
        if ((item is null && handAction is null) || equipmentTargets is null)
            frame.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left).Invoke(action);
        var icon = item?.Icon ?? placeholder;
        if (icon is null && item is not null)
        {
            var kind = item.Slot switch
            {
                "Head" => "head", "Torso" => "torso", "Feet" => "feet",
                "Left hand" => "hand_left", "Right hand" => "hand_right",
                _ => "acc_pouch",
            };
            icon = $"ui.item_placeholder_{kind}.png";
        }
        if (icon is not null)
            frame.Children.Add(new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId(icon)),
                Width = 42f, Height = 42f, SizingMode = ImageSizingMode.Fit,
                Color = item is null ? new Color(1f, 1f, 1f, 0.6f) : Colors.White,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
        if ((item is null && handAction is null) || equipmentTargets is null) return frame;
        var dialog = _dialog!;
        ImageElement? dragIcon = null;
        void MoveDragIcon(Vector2D<float> position)
        {
            if (dragIcon is not null)
                dragIcon.Margins = new(Left: Math.Max(0f, position.X - 21f), Right: 0f,
                    Top: Math.Max(0f, position.Y - 21f), Bottom: 0f);
        }
        var cell = new AbilityDragCell(position =>
        {
            if (_dialog != dialog || _closeDialogRequested) return;
            dragIcon = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId(icon!)),
                Width = 42f, Height = 42f, SizingMode = ImageSizingMode.Fit,
                HorizontalAlignment = AlignHorizontal.Left, VerticalAlignment = AlignVertical.Top,
                SortOrder = 20000, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            dialog.Children.Add(dragIcon);
            MoveDragIcon(position);
        }, MoveDragIcon, position =>
        {
            if (dragIcon is not null) dialog.Children.Remove(dragIcon);
            dragIcon = null;
            if (_dialog != dialog || _closeDialogRequested || _pending is null) return;
            if (handAction is not null)
                for (var slotIndex = 0; slotIndex < _quickSlots.Count; slotIndex++)
                {
                    var bounds = _quickSlots[slotIndex].Bounds;
                    if (position.X < bounds.Origin.X || position.X >= bounds.Max.X
                        || position.Y < bounds.Origin.Y || position.Y >= bounds.Max.Y) continue;
                    Loadout.Assign(slotIndex, handAction.Id.Value);
                    RefreshCommand();
                    return;
                }
            if (item is null) return;
            foreach (var (slot, target) in equipmentTargets)
            {
                var bounds = target.Bounds;
                if (position.X < bounds.Origin.X || position.X >= bounds.Max.X
                    || position.Y < bounds.Origin.Y || position.Y >= bounds.Max.Y) continue;
                if (Loadout.TryEquip(_pending, item.Id.Value, slot))
                {
                    _inspectedItem = item.Id.Value;
                    RenderCharacterDialog();
                }
                break;
            }
        }, clicked: action)
        {
            Width = ItemCellSize, Height = ItemCellSize,
            HorizontalAlignment = AlignHorizontal.Left, VerticalAlignment = AlignVertical.Top,
        };
        cell.Children.Add(frame);
        return cell;
    }
    private void EditEquipmentSlot(string slot)
    {
        var item = Loadout.Inventory.Find(i => i.Id.Value == _inspectedItem);
        var pending = _pending!;
        if (item is null || !Loadout.TryEquip(pending, item.Id.Value, slot))
            _inspectedItem = pending.GetValueOrDefault(slot);
        RenderCharacterDialog();
    }
    private void RenderCharacterDialog()
    {
        var pending = _pending!; _dialog?.Dispose(); _dialog = null;
        var panel = BeginDialog(null);
        const float inventoryLeft = 24f;
        const float inventoryWidth = 4f * ItemCellSize + 3f * InventoryCellGap;
        const float sectionGap = 24f;
        const float dividerWidth = 12f;
        const float firstDividerX = inventoryLeft + inventoryWidth + sectionGap;
        const float equipmentLeft = firstDividerX + dividerWidth + sectionGap;
        const float equipmentWidth = 4f * ItemCellSize + 3f * 8f;
        const float secondDividerX = equipmentLeft + equipmentWidth + sectionGap;
        const float statsLeft = secondDividerX + dividerWidth + sectionGap;
        const float equipmentCenter = equipmentLeft + (equipmentWidth - ItemCellSize) * 0.5f;
        foreach (var x in new[] { firstDividerX, secondDividerX })
            Place(panel, new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("ui.divider_vertical.png")),
                Width = dividerWidth, Height = 320f, SizingMode = ImageSizingMode.Stretch,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            }, x, 20f);

        var equipmentTargets = new Dictionary<string, Element>();
        const int pageSize = 20;
        _inventoryPage = Math.Clamp(_inventoryPage, 0, Math.Max(0, (Loadout.Inventory.Count - 1) / pageSize));
        var grid = new GridLayout
        {
            Width = inventoryWidth, Height = 5f * ItemCellSize + 4f * InventoryCellGap,
            Rows = Enumerable.Repeat(GridSize.Absolute(ItemCellSize + InventoryCellGap), 4)
                .Append(GridSize.Absolute(ItemCellSize)).ToArray(),
            Columns = Enumerable.Repeat(GridSize.Absolute(ItemCellSize + InventoryCellGap), 3)
                .Append(GridSize.Absolute(ItemCellSize)).ToArray(),
        };
        for (var index = 0; index < pageSize; index++)
        {
            var inventoryIndex = _inventoryPage * pageSize + index;
            var item = inventoryIndex < Loadout.Inventory.Count ? Loadout.Inventory[inventoryIndex] : null;
            var cell = ItemCell(item, null, () =>
            {
                _inspectedItem = item?.Id.Value;
                RenderCharacterDialog();
            }, equipmentTargets);
            grid.SetCell(index / 4, index % 4, cell);
        }
        Place(panel, grid, inventoryLeft, 24f);
        if (Loadout.Inventory.Count > pageSize)
        {
            var previous = DialogButton("Previous", () => { _inventoryPage--; RenderCharacterDialog(); });
            previous.Width = 110f; previous.Height = 28f; previous.IsEnabled = _inventoryPage > 0;
            Place(panel, previous, 24f, 328f);
            var next = DialogButton("Next", () => { _inventoryPage++; RenderCharacterDialog(); });
            next.Width = 110f; next.Height = 28f; next.IsEnabled = (_inventoryPage + 1) * pageSize < Loadout.Inventory.Count;
            Place(panel, next, 154f, 328f);
        }
        (string Slot, string Icon, float X, float Y)[] slots =
        [
            ("Head", "head", equipmentCenter, 24f),
            ("Torso", "torso", equipmentCenter, 108f),
            ("Feet", "feet", equipmentCenter, 192f),
            ("Left hand", "hand_left", equipmentLeft, 108f),
            ("Right hand", "hand_right", equipmentLeft + equipmentWidth - ItemCellSize, 108f),
            ("Acc1", "acc_ring", equipmentLeft, 276f),
            ("Acc2", "acc_belt", equipmentLeft + 64f, 276f),
            ("Acc3", "acc_neck", equipmentLeft + 128f, 276f),
            ("Acc4", "acc_pouch", equipmentLeft + 192f, 276f),
        ];
        foreach (var slot in slots)
        {
            var item = Loadout.Inventory.Find(i => i.Id.Value == pending.GetValueOrDefault(slot.Slot));
            var cell = ItemCell(item, $"ui.item_placeholder_{slot.Icon}.png", () => EditEquipmentSlot(slot.Slot), equipmentTargets, slot.Slot);
            equipmentTargets.Add(slot.Slot, cell);
            Place(panel, cell, slot.X, slot.Y);
        }
        (string Icon, string Segment, int Value)[] stats =
        [
            ("stats.health_icon.png", "stats.health_bar_segment.png", _gameState.Health),
            ("stats.focus_icon.png", "stats.focus_bar_segment.png", Loadout.Focus),
        ];
        for (var index = 0; index < stats.Length; index++)
        {
            var y = 24f + index * 34f;
            var row = new GridLayout
            {
                Width = 220f, Height = 24f,
                Columns = [GridSize.Absolute(32f), GridSize.Relative()],
                Rows = [GridSize.Absolute(24f)],
            };
            row.SetCell(0, 0, new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId(stats[index].Icon)),
                Width = 24f, Height = 24f, SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
            var segments = new ResourceRow(_textures.GetOrCreate(new ContentId(stats[index].Segment)))
            {
                Height = 16f,
                HorizontalAlignment = AlignHorizontal.Left,
                VerticalAlignment = AlignVertical.Center,
            };
            segments.SetPoints(stats[index].Value, 14f, -2f);
            row.SetCell(0, 1, segments);
            Place(panel, row, statsLeft, y);
        }
        var statusY = 102f;
        foreach (var id in _gameState.StatusEffects)
        {
            var effect = _storyline.StatusEffects[id];
            Place(panel, new ImageElement
            {
                Texture = _textures.GetOrCreate(effect.Icon),
                Width = 32f, Height = 32f,
                SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            }, statsLeft, statusY);
            var name = new TextElement(effect.Name, _hudTextStyle)
            {
                Color = UiTheme.TextColor,
                Width = 200f, Height = 24f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            Place(panel, name, statsLeft + 40f, statusY);
            var description = new TextElement(effect.Description, _hudTextStyle)
            {
                Color = UiTheme.TextColor,
                Width = 200f, Height = 48f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            Place(panel, description, statsLeft + 40f, statusY + 24f);
            statusY += 80f;
        }
        if (_gameState.StatusEffects.Length == 0)
            Place(panel, CreateHudLabel("No status effects", AlignVertical.Top), statsLeft, statusY);
        var footerY = Math.Max(Loadout.Inventory.Count > pageSize ? 356f : 340f, statusY) + 16f;
        var selectedItem = Loadout.Inventory.Find(i => i.Id.Value == _inspectedItem);
        if (selectedItem is not null)
        {
            var contextY = footerY;
            Place(panel, CreateHudLabel(selectedItem.Name, AlignVertical.Top), 24f, contextY);
            Place(panel, DialogButton("Move to top", () =>
            {
                Loadout.Inventory.Remove(selectedItem); Loadout.Inventory.Insert(0, selectedItem);
                _inventoryPage = 0; RenderCharacterDialog();
            }), 24f, contextY + 24f);
            var unequip = DialogButton("Unequip", () =>
            {
                foreach (var slot in pending.Where(p => p.Value == selectedItem.Id.Value).Select(p => p.Key).ToArray()) pending.Remove(slot);
                RenderCharacterDialog();
            });
            unequip.IsEnabled = pending.Values.Contains(selectedItem.Id.Value);
            Place(panel, unequip, 230f, contextY + 24f);
            var actions = Loadout.Abilities.Where(a => a.RequiredItem == selectedItem.Id).ToArray();
            var y = contextY + 66f;
            var assignmentY = y + actions.Length * 40f;
            foreach (var action in actions)
            {
                Place(panel, CreateHudLabel($"{action.Name} / Cost {action.TurnCost} / Focus {action.FocusCost}", AlignVertical.Top), 250f, y);
                if (action.Learned && !action.Passive) AssignControls(panel, action, y, destinationY: assignmentY);
                y += 40f;
            }
            footerY = actions.Length > 0 ? assignmentY + 48f : contextY + 76f;
        }
        _dialog!.Content.Height = footerY + 56f;
        var message = CreateHudLabel("", AlignVertical.Top);
        Place(panel, message, 200f, footerY + 8f);
        Place(panel, CreateHudLabel($"Turn +{Loadout.Cost(pending):0.##}", AlignVertical.Top), 24f, footerY + 8f);
        if (Loadout.ChangedEquipmentSlots(pending).Count == 0)
            PlaceDialogFooterButton(panel, "Close", CloseDialog, 760f, footerY);
        else
        {
            PlaceDialogFooterButton(panel, "Cancel", CloseDialog, 638f, footerY);
            PlaceDialogFooterButton(panel, "Confirm", () =>
            {
                if (Loadout.Commit(Combat, "player", pending)) CloseDialog();
                else message.Text = "Cannot commit equipment now";
            }, 760f, footerY);
        }
    }
    public void OpenZodiacDialog()
    {
        if (_dialog is not null) return;
        RenderZodiacDialog();
    }
    private void RenderZodiacDialog()
    {
        _dialog?.Dispose(); _dialog = null;
        var panel = BeginDialog(null);
        const int columns = 3;
        const int rows = 5;
        const float cellWidth = 280f;
        const float cellHeight = 72f;
        _dialog!.Content.VerticalAlignment = AlignVertical.Top;
        _dialog.Content.Margins = new(Left: 0f, Right: 0f, Top: 72f, Bottom: 0f);
        var grid = new GridLayout
        {
            Width = columns * cellWidth, Height = rows * cellHeight,
            Columns = Enumerable.Repeat(GridSize.Absolute(cellWidth), columns).ToArray(),
            Rows = Enumerable.Repeat(GridSize.Absolute(cellHeight), rows).ToArray(),
        };
        var selectionFrames = new Dictionary<AbilityId, ImageElement>();
        for (var index = 0; index < columns * rows; index++)
        {
            if (index >= Database.Abilities.DisplayOrder.Count)
            {
                grid.SetCell(index / columns, index % columns, new Element());
                continue;
            }
            var ability = _storyline.Abilities[Database.Abilities.DisplayOrder[index].Id];
            var dialog = _dialog!;
            ImageElement? dragIcon = null;
            void MoveDragIcon(Vector2D<float> position)
            {
                if (dragIcon is not null)
                    dragIcon.Margins = new(Left: Math.Max(0f, position.X - 16f), Right: 0f,
                        Top: Math.Max(0f, position.Y - 16f), Bottom: 0f);
            }
            var cell = new AbilityDragCell(position =>
            {
                if (_dialog != dialog || _closeDialogRequested) return;
                dragIcon = new ImageElement
                {
                    Texture = _textures.GetOrCreate(ability.Icon),
                    Width = 32f, Height = 32f, SizingMode = ImageSizingMode.Fit,
                    HorizontalAlignment = AlignHorizontal.Left, VerticalAlignment = AlignVertical.Top,
                    SortOrder = 20000, RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                };
                dialog.Children.Add(dragIcon);
                MoveDragIcon(position);
            }, MoveDragIcon, position =>
            {
                if (dragIcon is not null) dialog.Children.Remove(dragIcon);
                dragIcon = null;
                if (_dialog != dialog || _closeDialogRequested) return;
                for (var slot = 0; slot < _quickSlots.Count; slot++)
                {
                    var bounds = _quickSlots[slot].Bounds;
                    if (position.X < bounds.Origin.X || position.X >= bounds.Max.X
                        || position.Y < bounds.Origin.Y || position.Y >= bounds.Max.Y) continue;
                    // Presentation placeholders can be assigned, but are unavailable until authored for combat.
                    if (Loadout.Find(ability.Id.Value) is null)
                        Loadout.Abilities.Add(new(ability.Id, ability.Name, ability.TurnCost,
                            Icon: ability.Icon.ToString()));
                    Loadout.Assign(slot, ability.Id.Value);
                    RefreshCommand();
                    break;
                }
            }, selected: () =>
            {
                if (_dialog != dialog || _closeDialogRequested) return;
                _selectedZodiacAbility = ability.Id;
                foreach (var (id, frame) in selectionFrames)
                    frame.IsVisible = id == ability.Id;
            });
            grid.SetCell(index / columns, index % columns, cell);
            var selectionFrame = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("ui.item_frame_selected.png")),
                Width = 56f, Height = 56f, SizingMode = ImageSizingMode.Stretch,
                IsVisible = _selectedZodiacAbility == ability.Id,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            selectionFrames.Add(ability.Id, selectionFrame);
            Place(cell, selectionFrame, 0f, 0f);
            Place(cell, new ImageElement
            {
                Texture = _textures.GetOrCreate(ability.Icon),
                Width = 32f, Height = 32f, SizingMode = ImageSizingMode.Fit,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            }, 12f, 12f);
            Place(cell, new TextElement(ability.Name, _hudTextStyle)
            {
                Color = UiTheme.TextColor,
                Width = 200f, Height = 24f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            }, 64f, 0f);
            Place(cell, new TextElement(ability.Description, _hudTextStyle)
            {
                Color = UiTheme.TextColor,
                Width = 200f, Height = 48f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            }, 64f, 24f);
        }
        Place(panel, grid, 24f, 24f);
        const float footerY = 24f + rows * cellHeight + 16f;
        _dialog!.Content.Height = footerY + 56f;
        PlaceDialogFooterButton(panel, "Close", CloseDialog, 760f, footerY);
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
        var image = new ImageElement
        {
            Texture = _textures.GetOrCreate(new ContentId("zodiac_signs")),
            SourceRegion = signs[0],
            Width = 68f,
            Height = 68f,
            HorizontalAlignment = AlignHorizontal.Right,
            VerticalAlignment = AlignVertical.Center,
            Margins = new(0f, 16f, 0f, 0f),
            SizingMode = ImageSizingMode.Fit,
            SortOrder = 1,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        image.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left).Invoke(OpenZodiacDialog);
        return image;
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
            Color = UiTheme.TextColor,
            Height = 20f,
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = alignment,
            Margins = new(10f, 10f, 8f, 8f),
            SortOrder = 2,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };

    /// <summary>Uses the complete standalone panel texture with fixed-size borders.</summary>
    private PanelElement CreatePanelElement(string contentId, bool square = false) =>
        new(_textures.GetOrCreate(new ContentId(contentId)),
            square ? new(32f, 32f, 32f, 32f) : new(48f, 32f, 48f, 32f))
        {
            Margins = new(3f, 3f),
        };

    /// <inheritdoc />
    public override void Update(double deltaTime)
    {
        if (_closeDialogRequested)
        {
            _closeDialogRequested = false; var dialog = _dialog; _dialog = null; dialog?.Dispose();
        }
        base.Update(deltaTime);
        if (FocusedCharacter is not null && CurrentTarget() is not { Exists: true }) FocusCharacter(null);
        if (Combat.ActiveCombatant is { PlayerControlled: true } actor && actor.Id != "player") SelectedActionId = null;
        RefreshCommand();

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
                        headY - 8f - symbolHeight * 0.5f - 22f, 132f, 66f));
                }
            }
        }
    }
}








