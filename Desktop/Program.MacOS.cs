using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nexus.GameEngine.Runtime;
using System.Diagnostics;

namespace NexusRealms.Prelude.Desktop;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            Debug.WriteLine("Starting NexusRealms Prelude for macOS...");

            // macOS-specific startup context
            var startupContext = new ApplicationContext
            {
                CommandLineArguments = args,
                Configuration = BuildMacOSConfiguration(args),
                RunMode = DetectMacOSRunMode(args),
                LoggingProvider = "os_log",
                Platform = PlatformType.MacOS
            };

            Debug.WriteLine("Translated macOS startup to application context");

            // Platform-agnostic desktop application creation
            var application = new Application();

            // Run application with translated context
            var applicationResult = await application.RunAsync(startupContext);

            // macOS-specific shutdown translation
            var exitCode = TranslateMacOSShutdownToOS(applicationResult);

            if (exitCode == 0)
            {
                Debug.WriteLine("NexusRealms Prelude (macOS) exited successfully.");
            }
            else
            {
                Debug.WriteLine($"NexusRealms Prelude (macOS) exited with code {exitCode}: {applicationResult.Message}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            return TranslateMacOSErrorToOS(ex);
        }
    }

    static int TranslateMacOSShutdownToOS(ApplicationResult result)
    {
        var exitCode = result.ExitReason switch
        {
            ExitReason.Success => 0,
            ExitReason.UserRequested => 0,
            ExitReason.ConfigurationError => 1,
            ExitReason.GraphicsInitError => 1,
            ExitReason.AudioInitError => 1,
            ExitReason.InputInitError => 1,
            ExitReason.ServiceRegistrationError => 1,
            ExitReason.UnhandledException => 1,
            _ => 1
        };

        Debug.WriteLine($"Translated application result to macOS exit code: {exitCode}");
        return exitCode;
    }

    static int TranslateMacOSErrorToOS(Exception exception)
    {
        Debug.WriteLine($"Fatal macOS startup error: {exception.Message}");
        Debug.WriteLine($"Stack trace: {exception.StackTrace}");
        return 1; // Generic error
    }

    static IConfiguration BuildMacOSConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.macos.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/NexusRealms/config.json", optional: true) // User config
            .AddEnvironmentVariables("NEXUS_")
            .AddCommandLine(args)
            .Build();
    }

    static RunMode DetectMacOSRunMode(string[] args)
    {
        // Check if running as an app bundle
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CFBundleIdentifier")))
        {
            return RunMode.AppBundle;
        }

        return RunMode.Console;
    }
}
