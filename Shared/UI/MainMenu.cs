using Nexus.GameEngine.Components;

namespace NexusRealms.Prelude.Shared.UI;

public static class MainMenuTemplates
{
    public static readonly RuntimeComponent.Template MainMenuTemplate = new()
    {
        // Set required properties here
        Name = "MainMenu",
        Subcomponents =
        [
            new BackgroundImageTemplate()
            {

            }
        ]
    };
}