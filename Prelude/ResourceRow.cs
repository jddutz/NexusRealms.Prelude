namespace NexusRealms.Prelude;

/// <summary>A resource row rendered with one standalone segment texture per point.</summary>
public sealed class ResourceRow : Element
{
    private readonly ITexture _texture;
    private readonly ITexture? _spentTexture;
    private readonly Dictionary<int, double> _spent = [];
    private int _current;
    private bool _initialized;
    private float _width;
    private float _gap;
    private const double FlashDuration = 0.08d;
    private const double FadeDuration = 0.32d;
    public double AnimationSpeed { get; set; } = 1d;
    public bool IsShowingLoss => _spent.Count > 0;
    public int DisplayPoints => _spent.Count == 0 ? _current : Math.Max(_current, _spent.Keys.Max() + 1);

    public ResourceRow(ITexture texture, bool animateLoss = false)
    {
        _texture = texture;
        if (!animateLoss) return;
        // Multiplying the colored artwork by white cannot brighten it. Preserve its
        // silhouette and shading in a separate near-white texture instead.
        var bytes = new byte[checked((int)texture.Count * 4)];
        texture.WriteTo(0, texture.Count, ColorFormatEnum.RGBA8UNorm, bytes);
        var pixels = new Color[checked((int)texture.Count)];
        for (var i = 0; i < pixels.Length; i++)
        {
            var shade = 0.9f + 0.08f * Math.Max(bytes[i * 4], Math.Max(bytes[i * 4 + 1], bytes[i * 4 + 2])) / 255f;
            pixels[i] = new(shade, shade, shade, bytes[i * 4 + 3] / 255f);
        }
        _spentTexture = new Texture(new ContentId(texture.ContentId.Value + ".spent"), texture.Width, texture.Height, pixels);
    }

    public void SetPoints(int points, float width, float gap)
    {
        points = Math.Max(0, points);
        if (_initialized && _spentTexture is not null && points < _current)
            for (var point = points; point < _current; point++) _spent[point] = 0d;
        // Recovered points become live immediately, including during a pending fade.
        foreach (var point in _spent.Keys.Where(point => point < points).ToArray()) _spent.Remove(point);
        _current = points;
        _initialized = true;
        _width = MathF.Max(0f, width);
        _gap = MathF.Max(-_width + 1f, gap);
        SynchronizeSegments();
    }

    public override void Update(double deltaTime)
    {
        foreach (var point in _spent.Keys.ToArray())
        {
            var elapsed = _spent[point] + deltaTime * AnimationSpeed;
            if (elapsed >= FlashDuration + FadeDuration) _spent.Remove(point);
            else _spent[point] = elapsed;
        }
        SynchronizeSegments();
        base.Update(deltaTime);
    }

    private void SynchronizeSegments()
    {
        var count = DisplayPoints;
        while (Children.Count > count) Children.RemoveAt(Children.Count - 1);
        while (Children.Count < count)
            Children.Add(new ImageElement
            {
                Texture = _texture,
                SizingMode = ImageSizingMode.Stretch,
                RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
            });
        for (var point = 0; point < count; point++)
        {
            var segment = (ImageElement)Children[point];
            var fading = _spent.TryGetValue(point, out var elapsed);
            segment.Texture = fading ? _spentTexture : _texture;
            segment.IsVisible = point < _current || fading;
            segment.Color = Colors.White.WithTransparency(fading
                ? (float)Math.Clamp(1d - (elapsed - FlashDuration) / FadeDuration, 0d, 1d) : 1f);
        }
        ArrangeSegments();
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        ArrangeSegments();
    }

    private void ArrangeSegments()
    {
        for (var point = 0; point < Children.Count; point++)
        {
            var segment = (ImageElement)Children[point];
            segment.SortOrder = SortOrder;
            segment.Arrange(new(Bounds.Origin.X + point * (_width + _gap), Bounds.Origin.Y, _width, Bounds.Size.Y));
        }
    }
}
