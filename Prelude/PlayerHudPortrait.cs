namespace NexusRealms.Prelude;

/// <summary>Standalone portrait overlapping the left edge of the center HUD.</summary>
public sealed class PlayerHudPortrait : ImageElement
{
    public const float PortraitSize = 168f;
    public const float LeftInset = 8f;
    public const float BottomInset = 10f;

    public PlayerHudPortrait(ITexture texture)
    {
        Texture = texture;
        Width = PortraitSize;
        Height = PortraitSize;
        HorizontalAlignment = AlignHorizontal.Left;
        VerticalAlignment = AlignVertical.Top;
        SizingMode = ImageSizingMode.Fit;
        SortOrder = 10;
        RenderLayerMask = Nexus.Graphics.RenderLayers.DefaultUI;
    }

    public override void Arrange(Rectangle<float> bounds)
    {
        // Expand beyond the side cell so the portrait overlaps the center panel.
        // Keep its bottom aligned with the center panel's bottom inset.
        base.Arrange(new(bounds.Origin.X + LeftInset,
            bounds.Origin.Y + bounds.Size.Y - PortraitSize - BottomInset,
            PortraitSize, PortraitSize));
    }
}
