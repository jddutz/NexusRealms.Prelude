using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Runtime;

namespace NexusRealms.Prelude.Mobile;

/// <summary>
/// Manages the mobile application lifecycle: startup, execution, and cleanup.
/// Handles cross-platform mobile concerns while delegating OS-specific details to Program.cs
/// </summary>
public class Application
{
    private IServiceProvider? _services;
    private IConfiguration? _configuration;
    private ApplicationContext? _startupContext;
    private ILogger<Application>? _logger;

    /// <summary>
    /// Runs the mobile application with the provided startup context from the OS
    /// </summary>
    /// <param name="startupContext">Platform-specific startup context translated by Program.cs</param>
    /// <returns>Application result for translation back to OS by Program.cs</returns>
    public async Task<ApplicationResult> RunAsync(ApplicationContext startupContext)
    {
        try
        {
            Console.WriteLine($"Starting mobile application for {startupContext.Platform} platform...");

            // Store the startup context
            _startupContext = startupContext;
            _configuration = startupContext.Configuration;

            // Initialize mobile-specific services
            var initResult = await InitializeAsync();
            if (initResult.ExitReason != ExitReason.Success)
            {
                return initResult;
            }

            _logger?.LogInformation("Mobile application initialized, starting core application...");

            // TODO: Initialize and run the actual mobile game application
            // For now, just simulate running by waiting a short time
            await Task.Delay(1000);
            _logger?.LogInformation("Mobile application simulation completed");

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mobile application execution error: {ex.Message}");
            _logger?.LogError(ex, "Mobile application execution error");
            return ApplicationResult.Error(ExitReason.UnhandledException, ex.Message, ex);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Initializes mobile-specific services and configuration
    /// </summary>
    private async Task<ApplicationResult> InitializeAsync()
    {
        try
        {
            Console.WriteLine($"Initializing mobile runtime configuration for {_startupContext?.Platform}...");

            // Create dependency injection container with mobile-specific services
            _services = ConfigureMobileServices(_configuration!);
            _logger = _services.GetRequiredService<ILogger<Application>>();

            _logger.LogInformation($"Services initialized using {_startupContext?.LoggingProvider} logging provider");

            // Initialize mobile-specific services
            var serviceResult = await InitializeServicesAsync();
            if (serviceResult.ExitReason != ExitReason.Success)
            {
                return serviceResult;
            }

            _logger.LogInformation("Mobile application services initialized successfully");
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mobile application initialization error: {ex.Message}");
            return ApplicationResult.Error(ExitReason.ServiceRegistrationError, ex.Message, ex);
        }
    }

    /// <summary>
    /// Initializes mobile-specific services in the correct order
    /// </summary>
    private Task<ApplicationResult> InitializeServicesAsync()
    {
        try
        {
            _logger?.LogInformation("Initializing mobile services...");

            // TODO: Add mobile-specific service initialization
            // Initialize touch input service
            // var touchInputService = _services.GetRequiredService<ITouchInputService>();
            // await touchInputService.InitializeAsync();
            // _logger?.LogInformation("Touch input service initialized");

            // TODO: Initialize mobile-specific graphics (OpenGL ES/Metal/Vulkan)
            // TODO: Initialize mobile-specific audio
            // TODO: Initialize mobile lifecycle management (pause/resume/background)
            // TODO: Initialize mobile platform services (notifications, GPS, etc.)

            _logger?.LogInformation("Mobile services initialization completed");
            return Task.FromResult(ApplicationResult.Success());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Mobile services initialization error");
            return Task.FromResult(ApplicationResult.Error(ExitReason.ServiceRegistrationError, ex.Message, ex));
        }
    }

    /// <summary>
    /// Configures mobile-specific services
    /// </summary>
    private static IServiceProvider ConfigureMobileServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        // TODO: Add mobile-specific services
        // services.AddSingleton<IMobileGraphicsService, ...>();
        // services.AddSingleton<IMobileAudioService, ...>();
        // services.AddSingleton<ITouchInputService, ...>();
        // services.AddSingleton<IMobileLifecycleService, ...>();

        return services.BuildServiceProvider();
    }

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
    /// Performs cleanup of services and resources
    /// </summary>
    private Task CleanupAsync()
    {
        try
        {
            _logger?.LogInformation("Cleaning up mobile application services...");

            if (_services == null)
            {
                _logger?.LogInformation("No services to clean up");
                return Task.CompletedTask;
            }

            // TODO: Dispose mobile-specific services
            // Dispose touch input services first
            // if (_services.GetService<ITouchInputService>() is IDisposable touchInputService)
            // {
            //     touchInputService.Dispose();
            //     _logger?.LogInformation("Touch input service disposed");
            // }

            // TODO: Dispose mobile-specific graphics services
            // TODO: Dispose mobile-specific audio services
            // TODO: Dispose mobile lifecycle services

            // Clean up DI container
            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
                _logger?.LogInformation("Service container disposed");
            }

            _logger?.LogInformation("Mobile application cleanup completed");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during mobile application cleanup");
            Console.WriteLine($"Error during mobile application cleanup: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
