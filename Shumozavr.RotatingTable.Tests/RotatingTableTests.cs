using FluentAssertions;
using FluentAssertions.Equivalency;
using Shumozavr.RotatingTable.Tests.Fixture;

namespace Shumozavr.RotatingTable.Tests;

public class RotatingTableTests : IClassFixture<RotatingTableFixture>
{
    private readonly RotatingTableFixture _fixture;

    public RotatingTableTests(RotatingTableFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SetAccelerationTest()
    {
        var expectedAcceleration = 5;
        await _fixture.Client.SetAcceleration(expectedAcceleration, CancellationToken.None);
        var acceleration = await _fixture.Client.GetAcceleration(CancellationToken.None);
        Assert.Equal(expectedAcceleration, acceleration);

        expectedAcceleration = 10;
        await _fixture.Client.SetAcceleration(expectedAcceleration, CancellationToken.None);
        acceleration = await _fixture.Client.GetAcceleration(CancellationToken.None);
        Assert.Equal(expectedAcceleration, acceleration);
    }

    [Fact]
    public async Task RotatingTest()
    {
        _fixture.Emulator.GetRotatingStep = _ => 5.5;
        _fixture.Emulator.RotatingDelay = () => Task.Delay(TimeSpan.FromMilliseconds(100));
        var positions = (await _fixture.Client.StartRotating(30, CancellationToken.None)).ToBlockingEnumerable();
        var precision = 0.1;
        positions.Should().BeEquivalentTo(
            [0, 5.5, 11, 16.5, 22.0, 27.5, 30],
            WithPrecision(precision));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopRotatingTest(bool softStop)
    {
        _fixture.Emulator.GetRotatingStep = _ => 5;

        var semaphoreCount = 3;
        var expectedStopPosition = semaphoreCount + 1;
        var semaphore = new SemaphoreSlim(semaphoreCount);
        _fixture.Emulator.RotatingDelay = () => semaphore.WaitAsync();

        var positions = new List<double>();
        await foreach (var position in await _fixture.Client.StartRotating(30, CancellationToken.None))
        {
            positions.Add(position);
            if (positions.Count == expectedStopPosition)
            {
                if (softStop)
                {
                    await _fixture.Client.SoftStop(CancellationToken.None);
                }
                else
                {
                    await _fixture.Client.Stop(CancellationToken.None);
                }

                semaphore.Release(semaphoreCount);
            }
        }

        var precision = 0.1;
        positions.Should().BeEquivalentTo(
            [0, 5, 10, 15, 20],
            WithPrecision(precision));
    }

    private static Func<EquivalencyAssertionOptions<double>, EquivalencyAssertionOptions<double>> WithPrecision(
        double precision)
    {
        return o => o
                   .Using<double>(
                        ctx => ctx.Subject
                                  .Should()
                                  .BeApproximately(ctx.Expectation, precision))
                   .WhenTypeIs<double>();
    }
}