using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shumozavr.Common;
using Shumozavr.RotatingTable.Client;
using Shumozavr.RotatingTable.Emulator;

namespace Shumozavr.RotatingTable.Tests.Fixture;

public class RotatingTableFixture
{
    public readonly IRotatingTableClient Client;
    public readonly RotatingTableEmulator Emulator;

    public RotatingTableFixture()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRotatingTable(configuration);

        var serviceProvider = services.BuildServiceProvider();

        Client = serviceProvider.GetRequiredService<IRotatingTableClient>();
        Emulator = serviceProvider.GetRequiredService<RotatingTableEmulator>();
    }
}