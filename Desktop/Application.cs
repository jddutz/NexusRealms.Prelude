using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Runtime;

namespace NexusRealms.Prelude.Desktop;

/// <summary>
/// Manages the desktop application lifecycle: startup, execution, and cleanup.
/// Handles cross-platform desktop concerns while delegating OS-specific details to Program.cs
/// </summary>
public class Application
{
    private IServiceProvider? _services;
    private IConfiguration? _configuration;
    private ApplicationContext? _startupContext;
    private ILogger? _logger;

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    public IServiceProvider? Services => _services;

    /// <summary>
    /// Gets the configuration
    /// </summary>
    public IConfiguration? Configuration => _configuration;

    /// <summary>
    /// Gets the startup context provided by the OS
    /// </summary>
    public ApplicationContext? StartupContext => _startupContext;

    /// <summary>
    /// Runs the desktop application with the provided startup context from the OS
    /// </summary>
    /// <param name="startupContext">Platform-specific startup context translated by Program.cs</param>
    /// <returns>Application result for translation back to OS by Program.cs</returns>
    public async Task<ApplicationResult> RunAsync(ApplicationContext startupContext)
    {
        try
        {
            // Store the startup context
            _startupContext = startupContext;
            _configuration = startupContext.Configuration;

            // Get strongly-typed logging configuration
            System.Diagnostics.Debug.WriteLine("=== Configuration Debug Info ===");
            System.Diagnostics.Debug.WriteLine($"Current Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");
            System.Diagnostics.Debug.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

            // Debug the entire configuration
            System.Diagnostics.Debug.WriteLine("All configuration keys:");
            foreach (var configItem in _configuration.AsEnumerable())
            {
                System.Diagnostics.Debug.WriteLine($"  {configItem.Key}: {configItem.Value}");
            }

            // Check if the section exists
            var consoleLoggingSection = _configuration.GetSection("ConsoleLogging");
            System.Diagnostics.Debug.WriteLine($"ConsoleLogging section exists: {consoleLoggingSection.Exists()}");

            // Also check if regular Logging section exists
            var loggingSection = _configuration.GetSection("Logging");
            System.Diagnostics.Debug.WriteLine($"Logging section exists: {loggingSection.Exists()}");

            if (consoleLoggingSection.Exists())
            {
                System.Diagnostics.Debug.WriteLine("ConsoleLogging section values:");
                foreach (var child in consoleLoggingSection.GetChildren())
                {
                    System.Diagnostics.Debug.WriteLine($"  {child.Key}: {child.Value}");
                }
            }

            var loggingConfig = consoleLoggingSection.Get<LoggingConfiguration>();
            if (loggingConfig == null)
            {
                System.Diagnostics.Debug.WriteLine("LoggingConfiguration is NULL - using defaults");
                loggingConfig = new LoggingConfiguration();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"LoggingConfiguration loaded:");
                System.Diagnostics.Debug.WriteLine($"  MinimumLevel: {loggingConfig.MinimumLevel}");
                System.Diagnostics.Debug.WriteLine($"  ShowTimestamp: {loggingConfig.ShowTimestamp}");
                System.Diagnostics.Debug.WriteLine($"  ShowContext: {loggingConfig.ShowContext}");
                System.Diagnostics.Debug.WriteLine($"  UseColors: {loggingConfig.UseColors}");
            }
            System.Diagnostics.Debug.WriteLine("=== End Configuration Debug ===");

            _logger = new ConsoleLogger(
                "NexusRealms.Prelude",
                loggingConfig
            );

            // Test the logger immediately after creation
            System.Diagnostics.Debug.WriteLine("Testing logger after creation...");
            _logger?.LogDebug("Logger test: This is a debug message");
            _logger?.LogInformation("Logger test: This is an information message");
            _logger?.LogWarning("Logger test: This is a warning message");

            _logger?.LogDebug($"Starting desktop application for {startupContext.Platform} platform...");

            // Initialize desktop-specific services
            _logger?.LogDebug($"Initializing desktop runtime configuration for {_startupContext?.Platform}...");

            // Create dependency injection container with desktop-specific services
            _services = new ServiceCollection()
                .AddGameEngineServices(_configuration, loggingConfig)
                .BuildServiceProvider();

            _logger?.LogDebug($"Services initialized using {_startupContext?.LoggingProvider} logging provider");

            // Get the game application from DI and run it
            if (_services == null)
            {
                throw new InvalidOperationException("Services not initialized");
            }

            var gameApplication = _services.GetRequiredService<IApplication>();
            await gameApplication.RunAsync();

            _logger?.LogDebug("Application completed");

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug($"Desktop application execution error: {ex.Message}");
            return ApplicationResult.Error(ExitReason.UnhandledException, ex.Message, ex);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Performs cleanup of services and resources
    /// </summary>
    private Task CleanupAsync()
    {
        try
        {
            _logger?.LogDebug("Cleaning up desktop application services...");

            if (_services == null)
            {
                _logger?.LogDebug("No services to clean up");
                return Task.CompletedTask;
            }

            // TODO: Re-add input service disposal when new UI system is implemented
            // Dispose desktop input services first
            // if (_services.GetService<IEnhancedUIInputManager>() is IDisposable enhancedInputManager)
            // {
            //     enhancedInputManager.Dispose();
            //     _logger?.LogDebug("Enhanced UI input manager disposed");
            // }

            // if (_services.GetService<IInputRouter>() is IDisposable inputRouter)
            // {
            //     inputRouter.Dispose();
            //     _logger?.LogDebug("Desktop input router disposed");
            // }

            // TODO: Dispose desktop-specific graphics services
            // TODO: Dispose desktop-specific audio services

            // Clean up DI container
            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
                _logger?.LogDebug("Service container disposed");
            }

            _logger?.LogDebug("Desktop application cleanup completed");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug($"Error during desktop application cleanup: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
