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
            Console.WriteLine("Starting NexusRealms Mobile for iOS...");

            // iOS-specific startup context
            var startupContext = new ApplicationContext
            {
                CommandLineArguments = args,
                Configuration = BuildiOSConfiguration(args),
                RunMode = RunMode.Console,
                LoggingProvider = "iOS os_log",
                Platform = PlatformType.iOS
            };

            Console.WriteLine("Translated iOS startup to application context");

            // Platform-agnostic mobile application creation
            var application = new Application();

            // Run application with translated context
            var applicationResult = await application.RunAsync(startupContext);

            // iOS-specific shutdown translation
            var exitCode = TranslateiOSShutdownToOS(applicationResult);

            if (exitCode == 0)
            {
                Console.WriteLine("NexusRealms Mobile (iOS) exited successfully.");
            }
            else
            {
                Console.WriteLine($"NexusRealms Mobile (iOS) exited with code {exitCode}: {applicationResult.Message}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            return TranslateiOSErrorToOS(ex);
        }
    }

    static int TranslateiOSShutdownToOS(ApplicationResult result)
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

        Console.WriteLine($"Translated application result to iOS exit code: {exitCode}");
        return exitCode;
    }

    static int TranslateiOSErrorToOS(Exception ex)
    {
        Console.WriteLine($"Fatal iOS startup error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return 1; // iOS standard error
    }

    static IConfiguration BuildiOSConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.ios.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            // iOS apps store config in Documents directory
            .AddJsonFile($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/config.json", optional: true)
            .AddEnvironmentVariables("NEXUS_")
            .AddCommandLine(args)
            .Build();
    }
}
