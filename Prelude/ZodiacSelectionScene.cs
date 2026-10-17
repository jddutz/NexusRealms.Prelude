namespace NexusRealms.Prelude;

/// <summary>Lets the player choose a Zodiac sign before the story begins.</summary>
public sealed class ZodiacSelectionScene : Scene
{
    private const float UnselectedOpacity = 0.25f;
    private readonly GameState _state;
    private readonly ISceneManager _scenes;
    private readonly List<(ZodiacSignId Id, ImageElement Image, TextElement Label)> _signImages = [];
    private ZodiacSignId _selectedZodiac;

    /// <summary>Creates the Zodiac selection screen.</summary>
    /// <param name="state">The mutable game state receiving the selected sign.</param>
    /// <param name="storyline">The registered graph used to resolve the starting scene.</param>
    /// <param name="textStyles">Provides built-in interface text styles.</param>
    /// <param name="textures">Provides the button background texture.</param>
    /// <param name="scenes">Requests the transition into the story.</param>
    /// <param name="windowService">Closes the application when selection is cancelled.</param>
    public ZodiacSelectionScene(
        GameState state,
        Storyline storyline,
        ITextStyleRegistry textStyles,
        ITextureRegistry textures,
        ISceneManager scenes,
        IWindowService windowService
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(storyline);
        ArgumentNullException.ThrowIfNull(textStyles);
        ArgumentNullException.ThrowIfNull(textures);
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(windowService);

        _state = state;
        _scenes = scenes;
        _selectedZodiac = state.Zodiac;
        MainCamera = new StaticCamera();

        var signStyle = textStyles.GetOrCreate(BuiltInFonts.Bold, 24f);
        var buttonStyle = textStyles.GetOrCreate(BuiltInFonts.Default, 18f);
        var buttonTexture = textures.GetOrCreate(new ContentId("ui.panel_small.png"));
        var signGrid = new GridLayout
        {
            Rows = [GridSize.Relative(), GridSize.Relative(), GridSize.Relative()],
            Columns = [GridSize.Relative(), GridSize.Relative(), GridSize.Relative(), GridSize.Relative()],
            Margins = new(36f, 36f, 8f, 8f),
        };

        for (var index = 0; index < ZodiacSigns.All.Length; index++)
        {
            var sign = ZodiacSigns.All[index];
            var opacity = sign.Id == _selectedZodiac ? 1f : UnselectedOpacity;
            var image = new ImageElement
            {
                Texture = textures.GetOrCreate(GetEmblemContentId(sign.Id)),
                Color = new Color(1f, 1f, 1f, opacity),
                SizingMode = ImageSizingMode.Fit,
                HorizontalAlignment = AlignHorizontal.Center,
                VerticalAlignment = AlignVertical.Center,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            image.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
                .Invoke(() => SelectZodiac(sign.Id));
            var label = new TextElement(sign.Name, signStyle)
            {
                Color = UiTheme.TextColor.WithTransparency(opacity),
                HorizontalAlignment = AlignHorizontal.Center,
                VerticalAlignment = AlignVertical.Center,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            };
            label.InputMap.OnMouseButtonReleased(MouseButtonEnum.Left)
                .Invoke(() => SelectZodiac(sign.Id));
            var cell = new GridLayout
            {
                Rows = [GridSize.Relative(), GridSize.Absolute(40f)],
                Columns = [GridSize.Relative()],
                Margins = new(8f),
            };
            cell[0, 0] = image;
            cell[1, 0] = label;
            _signImages.Add((sign.Id, image, label));
            signGrid[index / 4, index % 4] = cell;
        }

        var header = new TextElement(
            "Choose your Zodiac",
            textStyles.GetOrCreate(BuiltInFonts.Default, 32f)
        )
        {
            Color = UiTheme.TextColor,
            HorizontalAlignment = AlignHorizontal.Center,
            VerticalAlignment = AlignVertical.Center,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        };

        var footer = new GridLayout
        {
            Rows = [GridSize.Relative()],
            Columns = [GridSize.Relative(), GridSize.Relative()],
            Margins = new(180f, 180f, 4f, 12f),
        };
        footer[0, 0] = new TextButton
        {
            Label = "Cancel",
            Style = buttonStyle,
            TextColor = UiTheme.TextColor,
            Texture = buttonTexture,
            SourceBorders = new(48f, 32f, 48f, 32f),
            BorderScale = 0.2f,
            Width = 180f,
            Height = 44f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            Action = _ => windowService.CloseWindow(),
        };
        footer[0, 1] = new TextButton
        {
            Label = "Next",
            Style = buttonStyle,
            TextColor = UiTheme.TextColor,
            Texture = buttonTexture,
            SourceBorders = new(48f, 32f, 48f, 32f),
            BorderScale = 0.2f,
            Width = 180f,
            Height = 44f,
            RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            Action = _ =>
            {
                _state.Zodiac = _selectedZodiac;
                StorySceneRouter.LoadCurrentNode(_state, storyline, _scenes);
            },
        };

        var layout = new GridLayout
        {
            Rows = [GridSize.Absolute(64f), GridSize.Relative(), GridSize.Absolute(72f)],
            Columns = [GridSize.Relative()],
        };
        layout[0, 0] = header;
        layout[1, 0] = signGrid;
        layout[2, 0] = footer;
        Children.Add(layout);

        var uiView = new View
        {
            Camera = MainCamera,
            LayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            PreserveDrawOrder = true,
        };
        uiView.ViewComponent.EnableDepthTest = false;
        uiView.ViewComponent.EnableDepthWrite = false;
        Children.Add(uiView);
    }

    /// <summary>Changes the selected sign and refreshes the opacity of every sign cell.</summary>
    /// <param name="id">The identity of the newly selected sign.</param>
    private void SelectZodiac(ZodiacSignId id)
    {
        _selectedZodiac = id;
        _state.Zodiac = id;
        RefreshSelection();
    }

    /// <summary>Applies full opacity to the selected sign and quarter opacity to the others.</summary>
    private void RefreshSelection()
    {
        foreach (var (id, image, label) in _signImages)
        {
            var opacity = id == _selectedZodiac ? 1f : UnselectedOpacity;
            image.Color = new Color(1f, 1f, 1f, opacity);
            label.Color = UiTheme.TextColor.WithTransparency(opacity);
        }
    }

    /// <summary>Gets the content identifier for a sign's authored emblem texture.</summary>
    /// <param name="id">The sign whose emblem is requested.</param>
    /// <returns>The registered content identifier for the emblem.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is not a defined sign.</exception>
    private static ContentId GetEmblemContentId(ZodiacSignId id) =>
        id switch
        {
            ZodiacSignId.Sagittarius => new ContentId("zodiac.Saggitarius.png"),
            ZodiacSignId.Aries or ZodiacSignId.Taurus or ZodiacSignId.Gemini
                or ZodiacSignId.Cancer or ZodiacSignId.Leo or ZodiacSignId.Virgo
                or ZodiacSignId.Libra or ZodiacSignId.Scorpio or ZodiacSignId.Capricorn
                or ZodiacSignId.Aquarius or ZodiacSignId.Pisces =>
                new ContentId($"zodiac.{id}.png"),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Zodiac sign."),
        };
}
