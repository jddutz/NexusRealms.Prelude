using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

try
{
    var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{env}.json", optional: true)
        .AddJsonFile(".content/content-manifest.json")
        .AddCommandLine(args)
        .Build();

    var services = new ServiceCollection().AddSingleton<Storyline>();

    using var application = new Application(configuration, services);
    application.Run();

    return 0;
}
catch (Exception ex)
{
    var error = ex.ToString();
    Console.Error.WriteLine(error);
    Debug.WriteLine(error);
    return 1;
}
