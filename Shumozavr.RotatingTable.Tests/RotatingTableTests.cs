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
        _fixture.Emulator.GetRotatingStep = angleToRotate => angleToRotate / 5d;
        _fixture.Emulator.RotatingDelay = () => Task.Delay(TimeSpan.FromMilliseconds(300));
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

    [Theory]
    [InlineData(30, 5.5, new[] {0d, 5.5, 11, 16.5, 22.0, 27.5, 30})]
    [InlineData(1, 33, new[] {0d, 1})]
    [InlineData(100, 33, new[] {0d, 33, 66, 99, 100})]
    public async Task StartRotatingTest(double expectedAngle, double step, double[] expected)
    {
        _fixture.Emulator.GetRotatingStep = _ => step;

        _fixture.Emulator.RotatingDelay = () => Task.Delay(TimeSpan.FromMilliseconds(100));
        var positions = (await _fixture.Client.StartRotating(expectedAngle, CancellationToken.None)).ToBlockingEnumerable();
        var precision = 0.1;
        positions.Should().BeEquivalentTo(
            expected,
            WithPrecision(precision));
    }

    [Fact]
    public async Task CancelRotatingTest()
    {
        var semaphore = new SemaphoreSlim(3);
        _fixture.Emulator.GetRotatingStep = angleToRotate => 1;
        _fixture.Emulator.RotatingDelay = () => semaphore.WaitAsync();

        var cts = new CancellationTokenSource();
        var clientRotatingTask = _fixture.Client.Rotate(1000, cts.Token);

        Assert.False(_fixture.Emulator.RotatingCt!.IsCancellationRequested);
        Assert.False(_fixture.Emulator.RotatingTask!.IsCanceled);
        await Task.Delay(1000, CancellationToken.None);
        _fixture.Emulator.RotatingCt!.Token.Register(() => semaphore.Release());
        cts.Cancel();

        var lastPos = await clientRotatingTask;
        // 0, 1, 2, 3
        Assert.Equal(3, lastPos);

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
        var positionsStream = await _fixture.Client.StartRotating(30, CancellationToken.None);
        _fixture.Emulator.RotatingCt!.Token.Register(() => semaphore.Release());
        await foreach (var position in positionsStream)
        {
            positions.Add(position);
            if (positions.Count == expectedStopPosition)
            {
                await _fixture.Client.Stop(softStop, CancellationToken.None);
            }
        }

        var precision = 0.1;
        positions.Should().BeEquivalentTo(
            [0, 5, 10, 15],
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