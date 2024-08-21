namespace Shumozavr.REW.RotatingTableClient;

public interface IRotatingTableClient : IDisposable
{
    public Task<IAsyncEnumerable<double>> StartRotating(double angle, CancellationToken cancellationToken);
    /// <summary>
    /// То же самое, что и StartRotating, но ожидает конца вращения
    /// </summary>
    public Task Rotate(double angle, CancellationToken cancellationToken);
    public Task Stop(CancellationToken cancellationToken);
    public Task SoftStop(CancellationToken cancellationToken);
    public Task SetAcceleration(int acceleration, CancellationToken cancellationToken);
    public Task<int> GetAcceleration(CancellationToken cancellationToken);
}