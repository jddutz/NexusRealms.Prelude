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
    private readonly TextButton _escapeButton;
    private readonly Vector2D<float> _worldSize;
    private readonly GameState _gameState;
    private readonly ITextStyle _hudTextStyle;
    private readonly ITextStyle _confirmationTextStyle;
    private readonly IGraphicalUserInterface _gui;
    private readonly ISceneManager _sceneManager;
    private readonly ImageElement _playerFlash = new()
    {
        SortOrder = -50,
        IsVisible = false,
        SizingMode = ImageSizingMode.Stretch,
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
    };
    private readonly Queue<Color> _flashColors = new();
    private double _flashRemaining;
    private NexusRealms.Prelude.Combat.ActionCandidate? _enemyCandidate;
    private string? _enemyDecisionKey;
    private readonly Dictionary<Character, EnemyActionState> _enemyActions = [];
    private ActionData? _enemyAttack;
    private Character? _enemyActor;
    private double _enemyElapsed;
    private double _enemyDuration;
    private bool _enemyHit;
    private bool _outcomeHandled;
    public Combat.CombatSystem Combat { get; }
    /// <summary>Shared action resolution over scene-owned health and inventory.</summary>
    public Actions.CombatActionResolver ActionResolver { get; }
    private readonly Dictionary<string, Character> _runtimeCharacters = [];
    public int EncounterSeed { get; }
    public TimelineEventRenderer TimelineRenderer { get; }
    private View? _worldView;
    private PanelElement? _centerHudPanel;
    private TurnOrderStrip? _turnOrder;
    private readonly List<(
        Character Character,
        CharacterStatusDisplay Display
    )> _characterDisplays = [];
    private Character? _pointerTarget;
    private Character? _pressedTarget;
    public Character? FocusedCharacter { get; private set; }
    public event Action<Character?>? FocusChanged;
    private readonly Dictionary<string, ContentId> _turnPortraits = [];
    private PlayerHudPortrait? _playerPortrait;
    private PlayerStatusBars? _playerStatusBars;
    private PanelElement? _confirmationPanel;

    /// <summary>The player's committed loadout and free quick-slot customization.</summary>
    public Combat.CombatLoadout Loadout => _gameState.Loadout;
    public string? SelectedActionId { get; private set; }
    private int? _selectedQuickSlot;
    private ModalDialog? _dialog;
    private string? _pendingThrowItem;
    private string? _actionBeforeCharacterDialog;
    private int? _quickSlotBeforeCharacterDialog;
    private readonly Dictionary<string, string> _pendingReloads = [];
    private bool _closeDialogRequested;
    private DragItem? _dragPreview;
    private AbilityId? _selectedZodiacAbility;
    private Dictionary<string, string>? _pending;
    private ImageElement? _actionIcon;
    private string? _renderedIconKey;
    private string? _commandFeedback;
    private readonly List<ImageElement> _quickSlots = [];
    private readonly List<ImageElement> _quickSlotIcons = [];
    private TextElement? _actionLabel;

    private Combat.CommandTarget? CurrentTarget()
    {
        var pair = _runtimeCharacters.FirstOrDefault(pair => pair.Value == FocusedCharacter);
        if (pair.Key is null) return null;
        var target = ActionResolver.Characters[pair.Key];
        var occupied = ActionResolver.Characters.Values.Where(character => character.Team == CombatTeam.Enemies
            && Combat.IsAlive(character.Id) && character.Position is not null).Select(character => RowOf(character.Position!.Value)).ToHashSet();
        return new(Combat.Contains(pair.Key), target.Health.Health > 0, target.Team, RowOf(target.Position!.Value),
            occupied.Contains(CombatRow.Front), occupied.Contains(CombatRow.Middle));
    }
    private static CombatRow RowOf(FormationSlot slot) =>
        slot >= FormationSlot.FrontLeft ? NexusRealms.Prelude.DataModel.CombatRow.Front
        : slot >= FormationSlot.MiddleLeft ? NexusRealms.Prelude.DataModel.CombatRow.Middle
        : NexusRealms.Prelude.DataModel.CombatRow.Back;

    private bool ValidTarget() =>
        Loadout.ValidTarget(Loadout.Find(SelectedActionId), CurrentTarget());

    public void SelectQuickAction(int slot)
    {
        if (_dialog is not null || (uint)slot >= Loadout.QuickSlots.Length)
            return;
        if (!Loadout.CanSelectQuickSlot(slot))
        {
            if (Loadout.QuickSlots[slot] == "Throw")
                _commandFeedback = Loadout.HasFreeHand
                    ? "No throwable items"
                    : "No accessible throwable items";
            return;
        }
        _commandFeedback = null;
        _selectedQuickSlot = slot;
        SelectedActionId = Loadout.QuickSlots[slot];
        RefreshCommand();
    }

    public bool ConfirmCombatAction()
    {
        var target = FocusedCharacter;
        if (_dialog is not null)
            return false;
        if (SelectedActionId is { } actionId && Loadout.Actions.TryGetValue(actionId, out var action))
        {
            var targetId = _runtimeCharacters.FirstOrDefault(pair => pair.Value == target).Key;
            if (!ActionResolver.CanCommit("player", action, targetId)) return false;
            ActionResolver.Commit("player", action, targetId);
            RefreshCommand();
            return true;
        }
        if (SelectedActionId == NexusRealms.Prelude.Combat.CombatLoadout.Throw.Id.Value)
        {
        var thrownItem = Loadout.Items.Find(item =>
                item.Id.Value == Loadout.PreparedThrowItemId
            );
            var confirmed = Loadout.ConfirmPreparedThrow(
                Combat,
                "player",
                CurrentTarget,
                damage =>
                {
                    if (target is not null)
                        ResolvePlayerDamage(
                            target,
                            damage,
                            impact: thrownItem is null
                                ? null
                                : NexusRealms.Prelude.Combat.AttackImpact.ForThrow(thrownItem)
                        );
                }
            );
            if (confirmed && Loadout.PreparedThrowItemId is null)
            {
                SelectedActionId = null;
                _selectedQuickSlot = null;
            }
            RefreshCommand();
            return confirmed;
        }
        return _dialog is null
            && Loadout.Confirm(
                Combat,
                "player",
                Loadout.Find(SelectedActionId),
                CurrentTarget,
                () => { },
                attack =>
                {
                    if (target is not null)
                        ResolvePlayerDamage(
                            target,
                            attack.DamagePoints,
                            impact: NexusRealms.Prelude.Combat.AttackImpact.ForStrike(attack)
                        );
                }
            );
    }

    /// <summary>Adapts committed strikes and throws to shared action resolution and feedback.</summary>
    private void ResolvePlayerDamage(Character target, DamageProfile damage, Combat.AttackImpact? impact)
    {
        var id = _runtimeCharacters.Single(pair => pair.Value == target).Key;
        ActionResolver.ResolveDamage("player", id, SelectedActionId ?? "attack", damage, impact);
    }

    private void OnDamageTaken(Character target, Combat.DamageTaken result)
    {
        if (!result.IsLethal)
            return;
        var id = _runtimeCharacters.FirstOrDefault(pair => pair.Value == target).Key;
        if (id is not null) Combat.Remove(id);
        if (FocusedCharacter == target)
            FocusCharacter(null);
    }

    private void RefreshCommand()
    {
        if (
            _selectedQuickSlot is { } selectedSlot
            && (
                !Loadout.CanSelectQuickSlot(selectedSlot)
                || Loadout.QuickSlots[selectedSlot] != SelectedActionId
            )
        )
        {
            _selectedQuickSlot = null;
            SelectedActionId = null;
        }
        var a = Loadout.Find(SelectedActionId);
        if (_actionLabel is not null)
            _actionLabel.Text =
                a is { RequiresTarget: true } && FocusedCharacter is null ? "Select a Target"
                : a is { RequiresTarget: true } && !ValidTarget() ? "Invalid Target"
                : a?.Hand is { } hand
                    ? Loadout.HeldItem(hand)?.Weapon is { Shots: > 0 } ? "Shoot"
                        : "Attack"
                : a?.Name ?? "Choose an Action";
        var actionTexture =
            a == NexusRealms.Prelude.Combat.CombatLoadout.Throw
            && _pendingThrowItem is { } pendingItem
                ? Loadout.Items.Find(item => item.Id.Value == pendingItem)?.Icon ?? a.Icon
            : a is null ? null
            : Loadout.CommandIcon(a);
        var valid =
            Loadout.CanConfirm(Combat, "player", a, ValidTarget())
            && (
                a != NexusRealms.Prelude.Combat.CombatLoadout.Throw || Loadout.CanThrowPreparedItem
            );
        if (_actionIcon is not null)
        {
            _actionIcon.IsVisible = actionTexture is not null;
            _actionIcon.Color = new Color(1f, 1f, 1f, valid ? 1f : 0.4f);
            if (
                a is not null
                && actionTexture is { } icon
                && _renderedIconKey != $"{icon}:{a.IconRegion}"
            )
            {
                _renderedIconKey = $"{icon}:{a.IconRegion}";
                _actionIcon.SourceRegion = null;
                _actionIcon.Texture = _textures.GetOrCreate(new ContentId(icon));
                if (a.IconRegion is { } region)
                    _actionIcon.SourceRegion = _actionIcon.Texture.GetRegion(region).Bounds;
            }
        }
        for (var i = 0; i < _quickSlots.Count; i++)
        {
            var q = Loadout.Find(Loadout.QuickSlots[i]);
            _quickSlots[i].Texture = _textures.GetOrCreate(
                new ContentId(
                    q is not null && _selectedQuickSlot == i && q.Id.Value == SelectedActionId
                        ? "ui.item_frame_selected.png"
                        : "ui.item_frame.png"
                )
            );
            var slotIcon = _quickSlotIcons[i];
            var quickTexture = q is null ? null : Loadout.CommandIcon(q);
            slotIcon.Color = new Color(
                1f,
                1f,
                1f,
                q is not null && Loadout.Available(q) ? 1f : 0.4f
            );
            slotIcon.SourceRegion = null;
            if (q is not null && quickTexture is { } slotTexture)
            {
                slotIcon.Texture = _textures.GetOrCreate(new ContentId(slotTexture));
                if (q.IconRegion is { } region)
                    slotIcon.SourceRegion = slotIcon.Texture.GetRegion(region).Bounds;
            }
            slotIcon.IsVisible = quickTexture is not null;
        }
        FocusedCharacter?.SetFocus(true, a is null || !a.RequiresTarget || ValidTarget());
        foreach (var (character, display) in _characterDisplays)
            display.Eligibility = _commandFeedback ?? "";
    }

    /// <summary>Creates the opening combat scene and its input bindings.</summary>
    /// <param name="textures">Provides background and character artwork.</param>
    /// <param name="eventHub">Dispatches the scene's input bindings.</param>
    /// <param name="windowService">Provides the main window closed by the exit bindings.</param>
    /// <param name="storyline">Provides the starting scenario and character definitions.</param>
    /// <param name="textStyles">Provides the style used by the Escape button.</param>
    public CombatScene(
        ITextureRegistry textures,
        IEventHub eventHub,
        IWindowService windowService,
        Storyline storyline,
        ITextStyleRegistry textStyles,
        GameState gameState,
        IGraphicalUserInterface gui,
        ISceneManager sceneManager,
        IContentManifest? contentManifest = null
    )
    {
        _textures = textures;
        _storyline = storyline;
        _windowService = windowService;
        _gameState = gameState;
        _gui = gui;
        _sceneManager = sceneManager;
        _hudTextStyle = textStyles.GetOrCreate(BuiltInFonts.Default, 16f);
        _confirmationTextStyle = textStyles.GetOrCreate(BuiltInFonts.Default, 12f);
        TimelineRenderer = new(CreateTimelineElement);
        TimelineRenderer.Changed += RefreshTurnOrder;

        if (!storyline.Nodes.TryGetValue(gameState.CurrentStoryNodeId, out var startNode))
        {
            throw new InvalidOperationException(
                $"The current story node '{gameState.CurrentStoryNodeId}' is not registered."
            );
        }

        _startScenario =
            startNode as CombatScenario
            ?? throw new InvalidOperationException(
                $"The current story node '{gameState.CurrentStoryNodeId}' is not a combat scenario."
            );

        EncounterSeed = _startScenario.RandomSeed ?? Random.Shared.Next();
        Combat = new(unchecked((uint)EncounterSeed));
        ActionResolver = new(Combat, storyline.StatusEffects);
        foreach (var ability in storyline.Abilities.Values) ActionResolver.RegisterAbility(ability);
        var playerHealth = new Combat.DamageReceiver(gameState.Health);
        playerHealth.HealthChanged += () => gameState.Health = playerHealth.Health;
        ActionResolver.Add(new("player", true, CombatTeam.PlayerAndAllies, playerHealth, gameState.Loadout), gameState.StatusEffects);
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
        _escapeButton = new TextButton
        {
            Label = "Escape",
            TextColor = UiTheme.TextColor,
            Action = _ => Combat.RequestEscape("player"),
            Width = 130f,
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
            LabelMargins = new(44f, 0f, 0f, 0f),
            Icon = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("icons.escape.png")),
                Width = 32f,
                Height = 28f,
                SizingMode = ImageSizingMode.Fit,
                HorizontalAlignment = AlignHorizontal.Left,
                VerticalAlignment = AlignVertical.Center,
                Margins = new(6f, 0f, 0f, 0f),
                Color = UiTheme.TextColor,
            },
            Margins = new(0f, 10f, 10f, 0f),
        };
        foreach (var placement in _startScenario.Characters)
        {
            var character = _storyline.Characters[placement.CharacterId];
            if (placement.Team == NexusRealms.Prelude.DataModel.CombatTeam.Enemies)
                character = character with
                {
                    Health = startingStats.Next(3, 8),
                    Focus = startingStats.Next(0, 4),
                };
            var combatCharacter = new Character(
                character,
                _textures.GetOrCreate(character.Artwork),
                _textures.GetOrCreate(new ContentId("shadow"))
            ) { StaggerAnimationEnabled = placement.Team != CombatTeam.Enemies };
            _enemyActions.Add(combatCharacter, new EnemyActionState(character, combatCharacter.Loadout));
            combatCharacter.ConfigureAnimations(contentManifest?.GetSpriteSheet(character.Artwork));
            combatCharacter.ConfigureEffectOverlays(_textures, _worldLayerMask);
            combatCharacter.Renderer.RenderLayerMask = _worldLayerMask;
            combatCharacter.ShadowRenderer.RenderLayerMask = _worldLayerMask;
            combatCharacter.ConfigureFocusIndicator(
                _textures.GetOrCreate(new ContentId("ui.selection_indicator.png")),
                _textures.GetOrCreate(new ContentId("ui.invalid_selection.png")),
                _worldLayerMask
            );
            _formation.SetSlot(placement.Slot, combatCharacter);
            var display = new CharacterStatusDisplay(combatCharacter, _textures, _hudTextStyle)
            {
                Width = 132f,
                Height = 66f,
                SortOrder = 10,
                IsVisible = false,
            };
            _characterDisplays.Add((combatCharacter, display));
            combatCharacter.DamageReceiver.DamageTaken += result =>
                OnDamageTaken(combatCharacter, result);
        }

        _turnPortraits.Add("player", _gameState.Portrait);
        Combat.Add(
            new Combat.Combatant("player", true, initiative: _startScenario.PlayerInitiative)
        );
        for (var index = 0; index < _startScenario.Characters.Length; index++)
        {
            var placement = _startScenario.Characters[index];
            var id = $"encounter-{index}";
            _turnPortraits.Add(id, _storyline.Characters[placement.CharacterId].Portrait);
            var row = placement.Slot switch
            {
                <= FormationSlot.BackRight => NexusRealms.Prelude.DataModel.CombatRow.Back,
                <= FormationSlot.MiddleRight => NexusRealms.Prelude.DataModel.CombatRow.Middle,
                _ => NexusRealms.Prelude.DataModel.CombatRow.Front,
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
            var visual = _formation[placement.Slot]!;
            _runtimeCharacters[id] = visual;
            ActionResolver.Add(new(id, false, placement.Team, visual.DamageReceiver, _enemyActions[visual].Loadout, placement.Slot), visual.Definition.StatusEffects);
        }
        ActionResolver.Moved += (id, destination) =>
        {
            var character = _runtimeCharacters[id];
            var previous = Enum.GetValues<FormationSlot>().Single(slot => _formation[slot] == character);
            _formation.SetSlot(previous, null);
            _formation.SetSlot(destination, character);
        };

        ActionResolver.ConfigureCharacters(_runtimeCharacters.ToDictionary(pair => pair.Key, pair => pair.Value.Definition));
        // Automatic turns yield to scene playback.
        foreach (var occurrence in _startScenario.Events)
            ActionResolver.ScheduleEvent(occurrence);
        Combat.IsCombatantAlive = id =>
        {
            if (id == "player")
                return _gameState.Health > 0;
            if (
                !id.StartsWith("encounter-", StringComparison.Ordinal)
                || !int.TryParse(id["encounter-".Length..], out var index)
                || (uint)index >= _startScenario.Characters.Length
            )
                return true;
            return _runtimeCharacters[$"encounter-{index}"].Health > 0;
        };
        _startScenario.ConfigureCombat(Combat);
        Combat.Changed += RefreshTurnOrder;
        Combat.Changed += () =>
        {
            if (Combat.HasEnded)
                FocusCharacter(null);
        };
        Combat.Process();

        var inputMap = new InputMap(eventHub, HitTestTargetSelection);
        inputMap
            .OnMouseButtonPressed(MouseButtonEnum.Left)
            .Invoke(() => _pressedTarget = _pointerTarget);
        inputMap
            .OnMouseButtonReleased(MouseButtonEnum.Left)
            .Invoke(() =>
            {
                if (_pressedTarget == _pointerTarget)
                    FocusCharacter(_pointerTarget);
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
        // This view composites transparent 2D sprites in draw order. Depth writes
        // must not let a formation sprite occlude a later damage overlay.
        worldView.ViewComponent.EnableDepthTest = false;
        worldView.ViewComponent.EnableDepthWrite = false;
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

        layout.SetCell(0, 2, _escapeButton);
        var turnOrder = new TurnOrderStrip(
            _textures,
            _textures.GetOrCreate(new ContentId("shadow"))
        )
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
            Columns = Enumerable
                .Repeat(
                    GridSize.Absolute(actionSlotSize + actionSlotGap),
                    Loadout.QuickSlots.Length - 1
                )
                .Append(GridSize.Absolute(actionSlotSize))
                .ToArray(),
            Rows = [GridSize.Absolute(actionSlotSize)],
        };
        for (var i = 0; i < Loadout.QuickSlots.Length; i++)
        {
            var slot = i;
            var frame = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("ui.item_frame.png")),
                Width = actionSlotSize,
                Height = actionSlotSize,
                HorizontalAlignment = AlignHorizontal.Left,
                SizingMode = ImageSizingMode.Stretch,
                SortOrder = 2,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            frame
                .InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
                .Invoke(() => SelectQuickAction(slot));
            var icon = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("actions.fist.png")),
                Width = 36f,
                Height = 36f,
                SizingMode = ImageSizingMode.Fit,
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
        bottomCenter.Children.Add(_playerStatusBars = new PlayerStatusBars(_gameState, _textures));
        bottomCenter.Children.Add(CreateZodiacImage());
        layout.SetCell(2, 1, bottomCenter);
        var bottomRight = CreatePanelElement("ui.panel_square.png", square: true);
        _confirmationPanel = bottomRight;
        bottomRight
            .InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
            .Invoke(() => ConfirmCombatAction());
        _actionIcon = CreateHudImage("actions.fist.png", 0.375f);
        bottomRight.Children.Add(_actionIcon);
        var confirmationLabel = CreateHudLabel("Click to Confirm", AlignVertical.Bottom);
        confirmationLabel.Style = _confirmationTextStyle;
        _actionLabel = confirmationLabel;
        bottomRight.Children.Add(confirmationLabel);
        RefreshCommand();
        layout.SetCell(2, 2, bottomRight);

        _playerFlash.Texture = _textures.GetOrCreate(new ContentId("uniform"));
        Children.Add(_playerFlash);
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
        foreach (var (_, display) in _characterDisplays)
        {
            display.View = _worldView;
            Children.Add(display);
        }
        ApplyWorldSize();
    }

    /// <summary>Called by the action selection flow once its effects and cost are chosen.</summary>
    public void SubmitAction(Combat.CombatAction action) => Combat.SubmitAction(action);

    /// <summary>Changes selection without resolving an action, including invalid targets.</summary>
    public void FocusCharacter(Character? character)
    {
        if (Combat.HasEnded)
            character = null;
        FormationSlot? selectedSlot = null;
        foreach (var slot in Enum.GetValues<FormationSlot>())
            if (character is not null && _formation[slot] == character)
                selectedSlot = slot;
        if (character is not null && selectedSlot is null)
            throw new ArgumentException("Character is not in this encounter.", nameof(character));
        if (FocusedCharacter == character)
            return;
        var previous = FocusedCharacter;
        FocusedCharacter = character;
        previous?.SetFocus(false, previous.IsValidTarget);
        character?.SetFocus(
            true,
            Loadout.Find(SelectedActionId) is not { RequiresTarget: true } || ValidTarget()
        );
        RefreshCommand();
        foreach (var (owner, display) in _characterDisplays)
            display.IsVisible = owner == character && !Combat.HasEnded;
        FocusChanged?.Invoke(character);
    }

    private bool HitTestTargetSelection(Vector2D<float> screenPosition)
    {
        _pointerTarget = null;
        if (_dialog is not null || Combat.HasEnded || _worldView is null)
            return false;
        bool Contains(Rectangle<float> rect) =>
            screenPosition.X >= rect.Origin.X
            && screenPosition.Y >= rect.Origin.Y
            && screenPosition.X < rect.Origin.X + rect.Size.X
            && screenPosition.Y < rect.Origin.Y + rect.Size.Y;
        if (
            !Contains(_worldView.Bounds)
            || (_centerHudPanel is { } center && Contains(center.Bounds))
            || (_playerPortrait is { } left && Contains(left.Bounds))
            || (_confirmationPanel is { } right && Contains(right.Bounds))
            || Contains(_escapeButton.Bounds)
            || (_turnOrder is { } order && screenPosition.Y < order.Bounds.Origin.Y + 130f)
        )
            return false;
        var viewport = _worldView.ViewComponent.ViewportRegion;
        if (viewport.Size.X <= 0 || viewport.Size.Y <= 0)
            return false;
        var world = new Vector2D<float>(
            (screenPosition.X - viewport.Origin.X) * _worldSize.X / viewport.Size.X,
            (screenPosition.Y - viewport.Origin.Y) * _worldSize.Y / viewport.Size.Y
        );
        // Hit the frontmost drawn opaque character when sprites overlap.
        foreach (var slot in Enum.GetValues<FormationSlot>().Reverse())
            if (
                _formation[slot] is { } candidate
                && candidate.Health > 0
                && candidate.HitTest(world)
            )
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
                isEnemy: id != "player"
                    && _startScenario.Characters[int.Parse(id["encounter-".Length..])].Team
                        == NexusRealms.Prelude.DataModel.CombatTeam.Enemies
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

    private TextButton DialogButton(string label, Action action) =>
        new()
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
        control.HorizontalAlignment = AlignHorizontal.Left;
        control.VerticalAlignment = AlignVertical.Top;
        control.Margins = new(Left: x, Right: 0f, Top: y, Bottom: 0f);
        panel.Children.Add(control);
    }

    private void CloseDialog() => CloseCharacterDialog(true);

    private void CloseCharacterDialog(bool activateThrow)
    {
        EndDragPreview();
        if (activateThrow && _pendingThrowItem is { } itemId && Loadout.PrepareThrow(itemId))
        {
            _selectedQuickSlot = null;
            SelectedActionId = NexusRealms.Prelude.Combat.CombatLoadout.Throw.Id.Value;
            RefreshCommand();
        }
        if (!activateThrow && _pendingThrowItem is not null)
        {
            SelectedActionId = _actionBeforeCharacterDialog;
            _selectedQuickSlot = _quickSlotBeforeCharacterDialog;
        }
        _pendingThrowItem = null;
        _pendingReloads.Clear();
        RefreshCommand();
        // Keep the scope alive through the current event dispatch, including Escape/Back.
        _pending = null;
        _closeDialogRequested = true;
    }

    private PanelElement BeginDialog(string? title)
    {
        _dialog = _gui.StartModalDialog();
        _dialog.Content.Width = 900f;
        _dialog.Content.Height = 460f;
        _dialog.InputMap.OnKeyPressed(KeyEnum.Escape).Invoke(CloseDialog);
        _dialog
            .InputMap.OnAnyControllerButtonPressed(ControllerSemanticNames.Back)
            .Invoke(CloseDialog);
        var panel = CreatePanelElement("ui.panel_large.png");
        _dialog.Content.Children.Add(panel);
        if (title is not null)
        {
            panel.Children.Add(CreateHudLabel(title, AlignVertical.Top));
        }
        return panel;
    }

    private void PlaceDialogFooterButton(
        Element panel,
        string label,
        Action action,
        float x,
        float y
    )
    {
        var button = DialogButton(label, action);
        button.Width = 110f;
        Place(panel, button, x, y);
    }

    public void OpenCharacterDialog()
    {
        if (_dialog is not null)
            return;
        _actionBeforeCharacterDialog = SelectedActionId;
        _quickSlotBeforeCharacterDialog = _selectedQuickSlot;
        _pending = Loadout.BeginItemLayout();
        _pendingThrowItem = null;
        _pendingReloads.Clear();
        RenderCharacterDialog();
    }

    private void StartDragPreview(ITexture texture, float size, IMouseInputDevice? mouse)
    {
        EndDragPreview();
        if (mouse is null)
            return;
        _dragPreview = new DragItem(texture, size, mouse);
        Children.Add(_dragPreview);
    }

    private void MoveDragPreview(Vector2D<float> _) => _dragPreview?.FollowMouse();

    private void EndDragPreview()
    {
        if (_dragPreview is not null)
            Children.Remove(_dragPreview);
        _dragPreview = null;
    }

    private const float ItemCellSize = 56f;
    private const float InventoryCellGap = 4f;

    private Element ItemCell(
        CarriedItem? item,
        string? placeholder,
        Action action,
        IReadOnlyDictionary<string, Element>? itemTargets = null,
        string? sourceSlot = null,
        Func<CarriedItem, Vector2D<float>, bool>? dropItem = null,
        List<Action>? refreshers = null,
        Action? refreshDialog = null
    )
    {
        // Scale the complete frame uniformly as a single image, rather than slicing its corners.
        var frame = new ImageElement
        {
            Texture = _textures.GetOrCreate(new ContentId("ui.item_frame.png")),
            Width = ItemCellSize,
            Height = ItemCellSize,
            SizingMode = ImageSizingMode.Stretch,
            HorizontalAlignment = AlignHorizontal.Left,
            VerticalAlignment = AlignVertical.Top,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };
        var handAction = NexusRealms.Prelude.Combat.CombatLoadout.HandAction(sourceSlot);
        string? icon = null;
        var visual = new ItemSlot();
        frame.Children.Add(visual);
        var count = CreateHudLabel("", AlignVertical.Bottom);
        count.HorizontalAlignment = AlignHorizontal.Right;
        count.Margins = new(Left: 0f, Right: 3f, Top: 0f, Bottom: 2f);
        frame.Children.Add(count);
        void RefreshSlot()
        {
            if (sourceSlot is not null && _pending is not null)
                item = Loadout.Items.Find(candidate =>
                    candidate.Id.Value == _pending.GetValueOrDefault(sourceSlot)
                );
            else if (sourceSlot is null && placeholder == "actions.throw.png")
                item = Loadout.Items.Find(candidate => candidate.Id.Value == _pendingThrowItem);
            icon = item?.Icon ?? placeholder;
            if (icon is null && item is not null)
            {
                var kind = item.Slot switch
                {
                    "Head" => "head",
                    "Torso" => "torso",
                    "Feet" => "feet",
                    "Left hand" => "hand_left",
                    "Right hand" => "hand_right",
                    _ => "acc_pouch",
                };
                icon = $"ui.item_placeholder_{kind}.png";
            }
            visual.SetIcon(
                icon is null ? null : _textures.GetOrCreate(new ContentId(icon)),
                item is null
            );
            count.Text = item is { Quantity: > 1 } ? item.Quantity.ToString() : "";
        }
        RefreshSlot();
        refreshers?.Add(RefreshSlot);
        bool CanDrag() => itemTargets is not null && (item is not null || handAction is not null);
        var dialog = _dialog!;
        AbilityDragCell? cell = null;
        cell = new AbilityDragCell(
            position =>
            {
                if (!CanDrag() || _dialog != dialog || _closeDialogRequested)
                    return;
                StartDragPreview(
                    _textures.GetOrCreate(new ContentId(icon!)),
                    42f,
                    cell!.CapturedMouse
                );
            },
            MoveDragPreview,
            position =>
            {
                EndDragPreview();
                if (!CanDrag() || _dialog != dialog || _closeDialogRequested || _pending is null)
                    return;
                if (handAction is not null)
                    for (var slotIndex = 0; slotIndex < _quickSlots.Count; slotIndex++)
                    {
                        var bounds = _quickSlots[slotIndex].Bounds;
                        if (
                            position.X < bounds.Origin.X
                            || position.X >= bounds.Max.X
                            || position.Y < bounds.Origin.Y
                            || position.Y >= bounds.Max.Y
                        )
                            continue;
                        Loadout.Assign(slotIndex, handAction.Id.Value);
                        RefreshCommand();
                        return;
                    }
                if (item is null)
                    return;
                if (dropItem?.Invoke(item, position) == true)
                    return;
                if (itemTargets is null)
                    return;
                foreach (var (slot, target) in itemTargets)
                {
                    var bounds = target.Bounds;
                    if (
                        position.X < bounds.Origin.X
                        || position.X >= bounds.Max.X
                        || position.Y < bounds.Origin.Y
                        || position.Y >= bounds.Max.Y
                    )
                        continue;
                    if (sourceSlot is not null && Loadout.TryMoveItem(_pending, sourceSlot, slot))
                    {
                        refreshDialog?.Invoke();
                    }
                    break;
                }
            },
            clicked: action
        )
        {
            Width = ItemCellSize,
            Height = ItemCellSize,
            HorizontalAlignment = AlignHorizontal.Left,
            VerticalAlignment = AlignVertical.Top,
        };
        cell.Children.Add(frame);
        return cell;
    }

    private void RenderCharacterDialog()
    {
        var pending = _pending!;
        _dialog?.Dispose();
        _dialog = null;
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
            Place(
                panel,
                new ImageElement
                {
                    Texture = _textures.GetOrCreate(new ContentId("ui.divider_vertical.png")),
                    Width = dividerWidth,
                    Height = 320f,
                    SizingMode = ImageSizingMode.Stretch,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                },
                x,
                20f
            );

        var itemTargets = new Dictionary<string, Element>();
        var refreshers = new List<Action>();
        void RefreshItems()
        {
            foreach (var weaponId in _pendingReloads.Keys.ToArray())
                if (!Loadout.CanReload(pending, weaponId, _pendingReloads[weaponId]))
                    _pendingReloads.Remove(weaponId);
            foreach (var refresh in refreshers)
                refresh();
        }
        var throwItem = Loadout.Items.Find(item => item.Id.Value == _pendingThrowItem);
        var throwFrame = ItemCell(
            throwItem,
            "actions.throw.png",
            () =>
            {
                _pendingThrowItem = null;
                SelectedActionId = _actionBeforeCharacterDialog;
                _selectedQuickSlot = _quickSlotBeforeCharacterDialog;
                RefreshItems();
                RefreshCommand();
            },
            refreshers: refreshers
        );
        bool DropItem(CarriedItem item, Vector2D<float> position)
        {
            static bool Contains(Element element, Vector2D<float> point) =>
                point.X >= element.Bounds.Origin.X
                && point.X < element.Bounds.Max.X
                && point.Y >= element.Bounds.Origin.Y
                && point.Y < element.Bounds.Max.Y;
            if (Contains(throwFrame, position))
            {
                if (Loadout.ThrowOptions(pending).Any(option => option.Item.Id == item.Id))
                {
                    _pendingThrowItem = item.Id.Value;
                    _selectedQuickSlot = null;
                    SelectedActionId = NexusRealms.Prelude.Combat.CombatLoadout.Throw.Id.Value;
                    RefreshItems();
                    RefreshCommand();
                }
                return true;
            }
            if (item.Ammunition is null)
                return false;
            foreach (var (slot, target) in itemTargets)
                if (
                    NexusRealms.Prelude.Combat.CombatLoadout.EquipmentSlots.Contains(slot)
                    && Contains(target, position)
                )
                {
                    var weaponId = pending.GetValueOrDefault(slot);
                    if (weaponId is not null && Loadout.CanReload(pending, weaponId, item.Id.Value))
                    {
                        _pendingReloads[weaponId] = item.Id.Value;
                        RefreshItems();
                    }
                    return true;
                }
            return false;
        }
        const int pageSize = NexusRealms.Prelude.Combat.CombatLoadout.InventoryCapacity;
        var grid = new GridLayout
        {
            Width = inventoryWidth,
            Height = 5f * ItemCellSize + 4f * InventoryCellGap,
            Rows = Enumerable
                .Repeat(GridSize.Absolute(ItemCellSize + InventoryCellGap), 4)
                .Append(GridSize.Absolute(ItemCellSize))
                .ToArray(),
            Columns = Enumerable
                .Repeat(GridSize.Absolute(ItemCellSize + InventoryCellGap), 3)
                .Append(GridSize.Absolute(ItemCellSize))
                .ToArray(),
        };
        for (var index = 0; index < pageSize; index++)
        {
            var inventorySlot = NexusRealms.Prelude.Combat.CombatLoadout.InventorySlot(index);
            var item = Loadout.Items.Find(item =>
                item.Id.Value == pending.GetValueOrDefault(inventorySlot)
            );
            var cell = ItemCell(
                item,
                null,
                () => { },
                itemTargets,
                inventorySlot,
                dropItem: DropItem,
                refreshers: refreshers,
                refreshDialog: RefreshItems
            );
            itemTargets.Add(inventorySlot, cell);
            grid.SetCell(index / 4, index % 4, cell);
        }
        Place(panel, grid, inventoryLeft, 24f);
        (string Slot, string Icon, float X, float Y)[] slots =
        [
            ("Head", "head", equipmentCenter, 24f),
            ("Torso", "torso", equipmentCenter, 108f),
            ("Feet", "feet", equipmentCenter, 192f),
            ("Left hand", "hand_right", equipmentLeft, 108f),
            ("Right hand", "hand_left", equipmentLeft + equipmentWidth - ItemCellSize, 108f),
            ("Acc1", "acc_ring", equipmentLeft, 276f),
            ("Acc2", "acc_belt", equipmentLeft + 64f, 276f),
            ("Acc3", "acc_neck", equipmentLeft + 128f, 276f),
            ("Acc4", "acc_pouch", equipmentLeft + 192f, 276f),
        ];
        foreach (var slot in slots)
        {
            var item = Loadout.Items.Find(i => i.Id.Value == pending.GetValueOrDefault(slot.Slot));
            var cell = ItemCell(
                item,
                $"ui.item_placeholder_{slot.Icon}.png",
                () => { },
                itemTargets,
                slot.Slot,
                dropItem: DropItem,
                refreshers: refreshers,
                refreshDialog: RefreshItems
            );
            itemTargets.Add(slot.Slot, cell);
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
                Width = 220f,
                Height = 24f,
                Columns = [GridSize.Absolute(32f), GridSize.Relative()],
                Rows = [GridSize.Absolute(24f)],
            };
            row.SetCell(
                0,
                0,
                new ImageElement
                {
                    Texture = _textures.GetOrCreate(new ContentId(stats[index].Icon)),
                    Width = 24f,
                    Height = 24f,
                    SizingMode = ImageSizingMode.Fit,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                }
            );
            var segments = new ResourceRow(
                _textures.GetOrCreate(new ContentId(stats[index].Segment))
            )
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
            Place(
                panel,
                new ImageElement
                {
                    Texture = _textures.GetOrCreate(effect.Icon),
                    Width = 32f,
                    Height = 32f,
                    SizingMode = ImageSizingMode.Fit,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                },
                statsLeft,
                statusY
            );
            var name = new TextElement(effect.Name, _hudTextStyle)
            {
                Color = UiTheme.TextColor,
                Width = 200f,
                Height = 24f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            Place(panel, name, statsLeft + 40f, statusY);
            var description = new TextElement(effect.Description, _hudTextStyle)
            {
                Color = UiTheme.TextColor,
                Width = 200f,
                Height = 48f,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            Place(panel, description, statsLeft + 40f, statusY + 24f);
            statusY += 80f;
        }
        if (_gameState.StatusEffects.Length == 0)
            Place(
                panel,
                CreateHudLabel("No status effects", AlignVertical.Top),
                statsLeft,
                statusY
            );
        var footerY = Math.Max(340f, statusY) + 16f;
        foreach (var weaponId in _pendingReloads.Keys.ToArray())
            if (!Loadout.CanReload(pending, weaponId, _pendingReloads[weaponId]))
                _pendingReloads.Remove(weaponId);
        _dialog!.Content.Height = footerY + ItemCellSize + 88f;
        throwFrame.HorizontalAlignment = AlignHorizontal.Left;
        throwFrame.VerticalAlignment = AlignVertical.Bottom;
        throwFrame.Margins = new(Left: 24f, Right: 0f, Top: 0f, Bottom: 24f);
        panel.Children.Add(throwFrame);
        var throwLabel = CreateHudLabel("Throw", AlignVertical.Bottom);
        throwLabel.Width = ItemCellSize;
        throwLabel.HorizontalAlignment = AlignHorizontal.Left;
        throwLabel.Margins = new(Left: 24f, Right: 0f, Top: 0f, Bottom: 24f + ItemCellSize + 4f);
        panel.Children.Add(throwLabel);
        var reloadLabel = CreateHudLabel("", AlignVertical.Top);
        Place(panel, reloadLabel, equipmentLeft, footerY);
        refreshers.Add(() =>
            reloadLabel.Text =
                _pendingReloads.Count > 0 ? $"Reload {_pendingReloads.Count} weapon(s)" : ""
        );
        var message = CreateHudLabel("", AlignVertical.Top);
        Place(panel, message, equipmentLeft, footerY + 24f);
        var equipmentCost = new Element
        {
            Width = equipmentWidth,
            Height = 36f,
            HorizontalAlignment = AlignHorizontal.Left,
            VerticalAlignment = AlignVertical.Bottom,
            Margins = new(Left: equipmentLeft, Right: 0f, Top: 0f, Bottom: 24f),
        };
        var costLabel = CreateHudLabel(
            $"Turn +{Loadout.ItemLayoutCost(pending, _pendingReloads):0.##}",
            AlignVertical.Center
        );
        costLabel.Margins = new(0f);
        equipmentCost.Children.Add(costLabel);
        panel.Children.Add(equipmentCost);
        TextButton FooterButton(string label, Action action, float rightMargin)
        {
            var button = DialogButton(label, action);
            button.Width = 110f;
            button.HorizontalAlignment = AlignHorizontal.Right;
            button.VerticalAlignment = AlignVertical.Bottom;
            button.Margins = new(Left: 0f, Right: rightMargin, Top: 0f, Bottom: 24f);
            panel.Children.Add(button);
            return button;
        }
        var closeButton = FooterButton("Close", CloseDialog, 24f);
        var cancelButton = FooterButton("Cancel", () => CloseCharacterDialog(false), 146f);
        var confirmButton = FooterButton(
            "Confirm",
            () =>
            {
                if (Loadout.CommitItemLayout(Combat, "player", pending, _pendingReloads))
                    CloseDialog();
                else
                    message.Text = "Cannot confirm changes now";
            },
            24f
        );
        refreshers.Add(() =>
        {
            var changed = Loadout.ChangedItemSlots(pending).Count > 0 || _pendingReloads.Count > 0;
            closeButton.IsVisible = !changed;
            cancelButton.IsVisible = confirmButton.IsVisible = changed;
            costLabel.Text = $"Turn +{Loadout.ItemLayoutCost(pending, _pendingReloads):0.##}";
            message.Text = "";
        });
        RefreshItems();
    }

    public void OpenZodiacDialog()
    {
        if (_dialog is not null)
            return;
        RenderZodiacDialog();
    }

    private void RenderZodiacDialog()
    {
        _dialog?.Dispose();
        _dialog = null;
        var panel = BeginDialog(null);
        const int columns = 4;
        const int rows = 5;
        const float cellWidth = 210f;
        const float cellHeight = 72f;
        _dialog!.Content.VerticalAlignment = AlignVertical.Top;
        _dialog.Content.Margins = new(Left: 0f, Right: 0f, Top: 72f, Bottom: 0f);
        var grid = new GridLayout
        {
            Width = columns * cellWidth,
            Height = rows * cellHeight,
            Columns = Enumerable.Repeat(GridSize.Absolute(cellWidth), columns).ToArray(),
            Rows = Enumerable.Repeat(GridSize.Absolute(cellHeight), rows).ToArray(),
        };
        var abilityOrder = Database.Progression.ProgressionGraphs.All[_gameState.Zodiac].Abilities.Select(node => node.Id).ToArray();
        var selectionFrames = new Dictionary<AbilityId, ImageElement>();
        for (var index = 0; index < columns * rows; index++)
        {
            if (index >= abilityOrder.Length)
            {
                grid.SetCell(index / columns, index % columns, new Element());
                continue;
            }
            var ability = _storyline.Abilities[abilityOrder[index]];
            var dialog = _dialog!;
            AbilityDragCell? cell = null;
            cell = new AbilityDragCell(
                position =>
                {
                    if (_dialog != dialog || _closeDialogRequested)
                        return;
                    StartDragPreview(_textures.GetOrCreate(ability.Icon), 32f, cell!.CapturedMouse);
                },
                MoveDragPreview,
                position =>
                {
                    EndDragPreview();
                    if (_dialog != dialog || _closeDialogRequested)
                        return;
                    for (var slot = 0; slot < _quickSlots.Count; slot++)
                    {
                        var bounds = _quickSlots[slot].Bounds;
                        if (
                            position.X < bounds.Origin.X
                            || position.X >= bounds.Max.X
                            || position.Y < bounds.Origin.Y
                            || position.Y >= bounds.Max.Y
                        )
                            continue;
                        var unlocked = ability.Actions.FirstOrDefault(action =>
                            Loadout.LearnedAbilities.Contains(ability.Id) && Loadout.Actions.ContainsKey(action.Id)
                            && Loadout.MeetsRequirements(Loadout.Actions[action.Id]));
                        if (unlocked is null) return;
                        Loadout.Assign(slot, unlocked.Id);
                        RefreshCommand();
                        break;
                    }
                },
                selected: () =>
                {
                    if (_dialog != dialog || _closeDialogRequested)
                        return;
                    _selectedZodiacAbility = ability.Id;
                    foreach (var (id, frame) in selectionFrames)
                        frame.IsVisible = id == ability.Id;
                }
            );
            grid.SetCell(index / columns, index % columns, cell);
            var selectionFrame = new ImageElement
            {
                Texture = _textures.GetOrCreate(new ContentId("ui.item_frame_selected.png")),
                Width = 56f,
                Height = 56f,
                SizingMode = ImageSizingMode.Stretch,
                IsVisible = _selectedZodiacAbility == ability.Id,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            selectionFrames.Add(ability.Id, selectionFrame);
            Place(cell, selectionFrame, 0f, 0f);
            Place(
                cell,
                new ImageElement
                {
                    Texture = _textures.GetOrCreate(ability.Icon),
                    Width = 32f,
                    Height = 32f,
                    SizingMode = ImageSizingMode.Fit,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                },
                12f,
                12f
            );
            Place(
                cell,
                new TextElement(ability.Name, _hudTextStyle)
                {
                    Color = UiTheme.TextColor,
                    Width = 200f,
                    Height = 24f,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                },
                64f,
                0f
            );
            Place(
                cell,
                new TextElement(ability.Description, _hudTextStyle)
                {
                    Color = UiTheme.TextColor,
                    Width = 200f,
                    Height = 48f,
                    RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
                },
                64f,
                24f
            );
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
            SourceRegion = signs[(int)_gameState.Zodiac],
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

    private ImageElement CreateHudImage(string contentId, float scale = 0.6f) =>
        new()
        {
            Texture = _textures.GetOrCreate(new ContentId(contentId)),
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
        new(
            _textures.GetOrCreate(new ContentId(contentId)),
            square ? new(32f, 32f, 32f, 32f) : new(48f, 32f, 48f, 32f)
        )
        {
            Margins = new(3f, 3f),
        };

    /// <inheritdoc />
    public override void Update(double deltaTime)
    {
        if (_closeDialogRequested)
        {
            _closeDialogRequested = false;
            var dialog = _dialog;
            _dialog = null;
            dialog?.Dispose();
        }
        foreach (var entry in _characterDisplays)
            entry.Character.AnimationSpeed = _gameState.GameSpeed;
        base.Update(deltaTime);
        AdvanceEnemyPlayback(deltaTime * _gameState.GameSpeed);
        if (FocusedCharacter is not null && CurrentTarget() is not { Exists: true })
            FocusCharacter(null);
        if (Combat.ActiveCombatant is { PlayerControlled: true } actor && actor.Id != "player")
            SelectedActionId = null;
        Combat.EvaluateOutcome();
        if (!Combat.HasEnded && Combat.IsEscaping && Combat.ActiveCombatant is null)
            Combat.Process();
        _escapeButton.IsEnabled =
            !Combat.HasEnded && !Combat.IsEscaping && Combat.ActiveCombatant?.Id == "player";
        if (
            !_outcomeHandled
            && Combat.Outcome is { } outcome
            && _playerStatusBars?.IsShowingLoss != true
            && _enemyAttack is null
            && !_playerFlash.IsVisible
            && _flashColors.Count == 0
            && !_characterDisplays.Any(entry => entry.Character.IsShowingFeedback)
        )
        {
            _outcomeHandled = true;
            _gameState.LastCombatOutcome = outcome;
            var destination = outcome switch
            {
                NexusRealms.Prelude.DataModel.CombatOutcome.Victory => _startScenario.VictoryNode,
                NexusRealms.Prelude.DataModel.CombatOutcome.Defeat => _startScenario.DefeatNode,
                _ => _startScenario.EscapeNode,
            };
            EndDragPreview();
            _dialog?.Dispose();
            _dialog = null;
            _pending = null;
            if (destination is { } nodeId)
            {
                _gameState.CurrentStoryNodeId = nodeId;
                if (_storyline.Nodes[nodeId] is CombatScenario)
                    _sceneManager.LoadScene<CombatScene>();
                else if (_storyline.Nodes[nodeId] is ProgressNode)
                    _sceneManager.LoadScene<ProgressionScene>();
                else
                    _sceneManager.LoadScene<OutcomeScene>();
            }
        }
        RefreshCommand();

        ApplyWorldSize();
    }

    private void AdvanceEnemyPlayback(double elapsed)
    {
        _flashRemaining -= elapsed;
        if (_flashRemaining <= 0d)
        {
            _playerFlash.IsVisible = _flashColors.TryDequeue(out var color);
            if (_playerFlash.IsVisible)
            {
                _playerFlash.Color = color.WithTransparency(0.3f);
                _flashRemaining = 0.08d;
            }
        }
        if (Combat.HasEnded)
        {
            _enemyAttack = null;
            return;
        }
        if (_enemyAttack is null)
        {
            if (_characterDisplays.Any(entry => entry.Character.IsShowingFeedback))
                return;
            if (Combat.ActiveCombatant is not { PlayerControlled: false } actor)
                return;
            var decision = NexusRealms.Prelude.Combat.CombatDecisionMaker.Decide(ActionResolver, actor.Id);
            if (decision.Selected is not { } candidate) { Combat.SubmitNoLegalAction(); return; }
            if (candidate.Kind == NexusRealms.Prelude.Combat.CandidateKind.ItemLayout && candidate.Reloads.Count == 0 || candidate.Action.Id == "wait")
            {
                ActionResolver.Commit(actor.Id, candidate);
                return;
            }
            _enemyActor = _runtimeCharacters[actor.Id];
            _enemyCandidate = candidate;
            _enemyDecisionKey = decision.StateKey;
            _enemyAttack = candidate.Action;
            if (
                !float.IsFinite(_enemyAttack.TurnCost)
                || _enemyAttack.TurnCost <= 0f
                || !float.IsFinite(_enemyAttack.ImpactFraction)
                || _enemyAttack.ImpactFraction < 0f
                || _enemyAttack.ImpactFraction > 1f
            )
                throw new InvalidOperationException("Enemy attack timing is invalid.");
            _enemyElapsed = 0d;
            _enemyDuration = 0d;
            _enemyHit = false;
            return;
        }
        var previous = _enemyElapsed;
        _enemyElapsed += elapsed;
        const double anticipation = 0.3d;
        if (previous <= anticipation && _enemyElapsed > anticipation && _enemyDuration == 0d)
        {
            _enemyDuration = _enemyActor!.PlayAttack(_enemyAttack.Animation);
            if (_enemyAttack.Reload)
                _enemyDuration = Math.Max(_enemyDuration, _enemyAttack.TurnCost);
            _enemyElapsed = anticipation;
        }
        if (_enemyDuration == 0d)
            return;
        if (
            !_enemyHit
            && _enemyElapsed >= anticipation + _enemyDuration * _enemyAttack.ImpactFraction
        )
        {
            _enemyHit = true;
            if (_enemyActor!.Health > 0 && Combat.ActiveCombatant is { } acting)
            {
                var selected = _enemyCandidate!;
                if (_enemyDecisionKey != NexusRealms.Prelude.Combat.CombatDecisionMaker.StateKey(ActionResolver) || !ActionResolver.CanCommit(acting.Id, selected))
                    selected = NexusRealms.Prelude.Combat.CombatDecisionMaker.Decide(ActionResolver, acting.Id).Selected;
                Combat.SuppressPump = true;
                try
                {
                    if (selected is null) Combat.SubmitNoLegalAction();
                    else
                    {
                        var result = ActionResolver.Commit(acting.Id, selected);
                        foreach (var effect in result.Effects.Where(effect => effect.RecipientId == "player" && effect.Requested is DamageEffect))
                            for (var i = 0; i < effect.ActualChange; i++)
                                _flashColors.Enqueue(Character.DamageColor(((DamageEffect)effect.Requested).Type));
                    }
                }
                finally { Combat.SuppressPump = false; }
            }
        }
        if (
            _enemyElapsed < anticipation + _enemyDuration + 0.4d
            || _flashColors.Count > 0
            || _playerFlash.IsVisible
        )
            return;
        _enemyAttack = null;
        _enemyCandidate = null;
        _enemyDecisionKey = null;
        Combat.Process();
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
                    var symbolWidth =
                        character.FocusRenderer.Instances.Values.First().Size.X
                        * character.Scale.X
                        * pixelsPerWorldX;
                    var symbolHeight =
                        character.FocusRenderer.Instances.Values.First().Size.Y
                        * character.Scale.Y
                        * pixelsPerWorldY;
                    display.LeftOffset = symbolWidth * 0.5f + 8f;
                    display.RightOffset = display.LeftOffset + 132f;
                    display.TopOffset =
                        -(character.VisualHeight + character.Elevation)
                            * character.Scale.Y
                            * pixelsPerWorldY
                        - 8f
                        - symbolHeight * 0.5f
                        - 22f;
                    display.BottomOffset = display.TopOffset + 66f;
                }
            }
        }
    }
}
