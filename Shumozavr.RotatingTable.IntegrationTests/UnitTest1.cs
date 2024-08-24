using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.RotatingTable.Client;
using Shumozavr.RotatingTable.Common;
using Shumozavr.RotatingTable.Emulator;

namespace Shumozavr.RotatingTable.IntegrationTests;

public class UnitTest1 : IClassFixture<RotatingTableFixture>
{
    private readonly RotatingTableFixture _fixture;

    public UnitTest1(RotatingTableFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test1()
    {
        var expectedAcceleration = 5;
        await _fixture.Client.SetAcceleration(expectedAcceleration, CancellationToken.None);
        var acceleration = await _fixture.Client.GetAcceleration(CancellationToken.None);
        Assert.Equal(expectedAcceleration, acceleration);
    }
}

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
        services.AddDefaultOptions<RotatingTableSettings>();
        services.AddRotatingTableClient(p => p.GetRequiredService<IOptions<RotatingTableSettings>>());
        services.AddRotatingTableEmulator(p => p.GetRequiredService<IOptions<RotatingTableSettings>>());
        var serviceProvider = services.BuildServiceProvider();

        Client = serviceProvider.GetRequiredService<IRotatingTableClient>();
        Emulator = serviceProvider.GetRequiredService<RotatingTableEmulator>();
    }
}