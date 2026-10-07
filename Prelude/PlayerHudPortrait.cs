namespace NexusRealms.Prelude;

/// <summary>Places the player's portrait slightly above its HUD panel.</summary>
public sealed class PlayerHudPortrait : ImageElement
{
    public PlayerHudPortrait(ITexture texture)
    {
        Texture = texture;
        Width = 128f;
        Height = 128f;
        HorizontalAlignment = AlignHorizontal.Left;
        VerticalAlignment = AlignVertical.Top;
        Margins = new(16f, 0f, 0f, 0f);
        SizingMode = ImageSizingMode.Fit;
        SortOrder = 1;
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI;
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        // Shift the allocation so the full image remains visible above the panel.
        base.Arrange(new(bounds.Origin.X, bounds.Origin.Y - 12f,
            bounds.Size.X, bounds.Size.Y));
    }
}
