namespace NexusRealms.Prelude;

/// <summary>Resolves authored icon identifiers against the active content manifest.</summary>
internal static class ContentIconResolver
{
    /// <summary>Returns the authored icon when packaged, or the supplied fallback when its artwork is absent.</summary>
    /// <param name="manifest">The active content manifest, if available.</param>
    /// <param name="icon">The authored icon identifier.</param>
    /// <param name="fallback">A packaged icon to use when the authored artwork is not present.</param>
    /// <returns>The authored icon or the fallback identifier.</returns>
    public static ContentId Resolve(IContentManifest? manifest, ContentId icon, ContentId fallback)
    {
        if (manifest is null)
            return icon;

        return manifest.Textures.GetSection("Content").GetSection(icon.Value).Exists()
            ? icon
            : fallback;
    }
}
