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
            Debug.WriteLine("Starting NexusRealms Prelude for Windows...");

            // Windows-specific startup context
            var startupContext = new ApplicationContext
            {
                CommandLineArguments = args,
                Configuration = BuildWindowsConfiguration(args),
                RunMode = DetectWindowsRunMode(args),
                LoggingProvider = "Windows Event Log",
                Platform = PlatformType.Windows
            };

            Debug.WriteLine("Translated Windows OS startup to application context");

            // Platform-agnostic desktop application creation
            var application = new Application();

            // Run application with translated context
            var applicationResult = await application.RunAsync(startupContext);

            // Windows-specific shutdown translation
            var exitCode = TranslateWindowsShutdownToOS(applicationResult);

            if (exitCode == 0)
            {
                Debug.WriteLine("NexusRealms Prelude (Windows) exited successfully.");
            }
            else
            {
                Debug.WriteLine($"NexusRealms Prelude (Windows) exited with code {exitCode}: {applicationResult.Message}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            return TranslateWindowsErrorToOS(ex);
        }
    }

    static int TranslateWindowsShutdownToOS(ApplicationResult result)
    {
        var exitCode = result.ExitReason switch
        {
            ExitReason.Success => 0,
            ExitReason.UserRequested => 0,
            ExitReason.ConfigurationError => 1,
            ExitReason.GraphicsInitError => 2,
            ExitReason.AudioInitError => 3,
            ExitReason.InputInitError => 4,
            ExitReason.ServiceRegistrationError => 5,
            ExitReason.UnhandledException => 6,
            _ => 1
        };

        Debug.WriteLine($"Translated application result to Windows exit code: {exitCode}");
        return exitCode;
    }

    static int TranslateWindowsErrorToOS(Exception exception)
    {
        Debug.WriteLine($"Fatal Windows startup error: {exception.Message}");
        Debug.WriteLine($"Stack trace: {exception.StackTrace}");
        return 10; // Custom Windows fatal error code
    }

    static IConfiguration BuildWindowsConfiguration(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.windows.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);

        // Add Windows Registry if available
        try
        {
            // Note: Would need custom configuration provider for registry
            // builder.Add(new RegistryConfigurationSource("HKEY_LOCAL_MACHINE\\SOFTWARE\\NexusRealms"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not access Windows Registry: {ex.Message}");
        }

        return builder
            .AddEnvironmentVariables("NEXUS_")
            .AddCommandLine(args)
            .Build();
    }

    static RunMode DetectWindowsRunMode(string[] args)
    {
        // Check for Windows Service mode
        if (Array.Exists(args, arg => arg.Equals("/service", StringComparison.OrdinalIgnoreCase) ||
                                      arg.Equals("--service", StringComparison.OrdinalIgnoreCase)))
        {
            return RunMode.WindowsService;
        }

        return RunMode.Console;
    }
}
