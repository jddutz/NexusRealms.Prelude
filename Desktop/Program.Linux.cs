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
            Debug.WriteLine("Starting NexusRealms Prelude for Linux...");

            // Linux-specific startup context
            var startupContext = new ApplicationContext
            {
                CommandLineArguments = args,
                Configuration = BuildLinuxConfiguration(args),
                RunMode = DetectLinuxRunMode(args),
                LoggingProvider = "syslog",
                Platform = PlatformType.Linux
            };

            Debug.WriteLine("Translated Linux OS startup to application context");

            // Platform-agnostic desktop application creation
            var application = new Application();

            // Run application with translated context
            var applicationResult = await application.RunAsync(startupContext);

            // Linux-specific shutdown translation
            var exitCode = TranslateLinuxShutdownToOS(applicationResult);

            if (exitCode == 0)
            {
                Debug.WriteLine("NexusRealms Prelude (Linux) exited successfully.");
            }
            else
            {
                Debug.WriteLine($"NexusRealms Prelude (Linux) exited with code {exitCode}: {applicationResult.Message}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            return TranslateLinuxErrorToOS(ex);
        }
    }

    static int TranslateLinuxShutdownToOS(ApplicationResult result)
    {
        var exitCode = result.ExitReason switch
        {
            ExitReason.Success => 0,
            ExitReason.UserRequested => 0,
            ExitReason.ConfigurationError => 78, // EX_CONFIG
            ExitReason.GraphicsInitError => 69,  // EX_UNAVAILABLE
            ExitReason.AudioInitError => 69,     // EX_UNAVAILABLE
            ExitReason.InputInitError => 69,     // EX_UNAVAILABLE
            ExitReason.ServiceRegistrationError => 70, // EX_SOFTWARE
            ExitReason.UnhandledException => 70, // EX_SOFTWARE
            _ => 1
        };

        Debug.WriteLine($"Translated application result to POSIX exit code: {exitCode}");
        return exitCode;
    }

    static int TranslateLinuxErrorToOS(Exception exception)
    {
        Debug.WriteLine($"Fatal Linux startup error: {exception.Message}");
        Debug.WriteLine($"Stack trace: {exception.StackTrace}");
        return 125; // Command invoked cannot execute
    }

    static IConfiguration BuildLinuxConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.linux.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("/etc/nexusrealms/config.json", optional: true) // System config
            .AddJsonFile($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.config/nexusrealms/config.json", optional: true) // User config
            .AddEnvironmentVariables("NEXUS_")
            .AddCommandLine(args)
            .Build();
    }

    static RunMode DetectLinuxRunMode(string[] args)
    {
        // Check for daemon mode
        if (Array.Exists(args, arg => arg.Equals("--daemon", StringComparison.OrdinalIgnoreCase)))
        {
            return RunMode.Daemon;
        }

        return RunMode.Console;
    }
}
