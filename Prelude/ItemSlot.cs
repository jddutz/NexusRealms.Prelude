namespace NexusRealms.Prelude;

/// <summary>Retains a slot icon renderer independently of inventory layout.</summary>
public sealed class ItemSlot : Element
{
    private readonly TextureRenderer _icon = new()
    {
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI,
        IsVisible = false,
    };

    public ItemSlot() => AddComponent(_icon);

    public void SetIcon(ITexture? texture, bool placeholder)
    {
        var color = placeholder ? new Color(1f, 1f, 1f, 0.6f) : Colors.White;
        if (_icon.Texture == texture && _icon.Color.Equals(color)) return;
        _icon.Texture = texture;
        _icon.Color = color;
        UpdateIcon();
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        base.Arrange(bounds);
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        _icon.IsVisible = IsVisible && _icon.Texture is not null && Bounds.Size.X > 0f && Bounds.Size.Y > 0f;
        if (_icon.Texture is not { } texture) return;
        var scale = MathF.Min(42f / texture.Width, 42f / texture.Height);
        var width = texture.Width * scale;
        var height = texture.Height * scale;
        _icon.Destination = new(Bounds.Origin.X + (Bounds.Size.X - width) * 0.5f,
            Bounds.Origin.Y + (Bounds.Size.Y - height) * 0.5f, width, height);
    }
}
