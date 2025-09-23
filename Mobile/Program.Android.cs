using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nexus.GameEngine.Runtime;

namespace NexusRealms.Prelude.Mobile;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting NexusRealms Mobile for Android...");

            // Android-specific startup context
            var startupContext = new ApplicationContext
            {
                CommandLineArguments = args,
                Configuration = BuildAndroidConfiguration(args),
                RunMode = RunMode.Console,
                LoggingProvider = "Android Logcat",
                Platform = PlatformType.Android
            };

            Console.WriteLine("Translated Android OS startup to application context");

            // Platform-agnostic mobile application creation
            var application = new Application();

            // Run application with translated context
            var applicationResult = await application.RunAsync(startupContext);

            // Android-specific shutdown translation
            var exitCode = TranslateAndroidShutdownToOS(applicationResult);

            if (exitCode == 0)
            {
                Console.WriteLine("NexusRealms Mobile (Android) exited successfully.");
            }
            else
            {
                Console.WriteLine($"NexusRealms Mobile (Android) exited with code {exitCode}: {applicationResult.Message}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            return TranslateAndroidErrorToOS(ex);
        }
    }

    static int TranslateAndroidShutdownToOS(ApplicationResult result)
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

        Console.WriteLine($"Translated application result to Android exit code: {exitCode}");
        return exitCode;
    }

    static int TranslateAndroidErrorToOS(Exception ex)
    {
        Console.WriteLine($"Fatal Android startup error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return 10; // Custom Android fatal error code
    }

    static IConfiguration BuildAndroidConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.android.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            // Android apps typically store config in app-specific directories
            .AddJsonFile("/data/data/com.nexusrealms.mobile/files/config.json", optional: true)
            .AddEnvironmentVariables("NEXUS_")
            .AddCommandLine(args)
            .Build();
    }
}
