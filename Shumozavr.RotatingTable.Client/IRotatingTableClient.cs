namespace Shumozavr.RotatingTable.Client;

public interface IRotatingTableClient
{
    /// <summary>
    /// Начинает вращение стола
    /// </summary>
    /// <param name="angle">Угол на который повернется стол</param>
    /// <param name="cancellationToken">Токен отменяет только инициацию команды. Для остановки уже запущенного стола надо вызвать Stop</param>
    /// <returns>Промежуточные углы поворота стола</returns>
    public Task<IAsyncEnumerable<double>> StartRotating(double angle, CancellationToken cancellationToken);

    /// <summary>
    /// То же самое что StartRotating, но с ожиданием окончания вращения.
    /// </summary>
    /// <param name="angle">Угол на который повернется стол</param>
    /// <param name="cancellationToken">Отмена команды также вызывает команду Stop и ожидает ее конца</param>
    /// <returns>Последнее состояние угла поворота стола</returns>
    public Task<double?> Rotate(double angle, CancellationToken cancellationToken);
    public Task Stop(bool softStop, CancellationToken cancellationToken);
    public Task SetAcceleration(int acceleration, CancellationToken cancellationToken);
    public Task<int> GetAcceleration(CancellationToken cancellationToken);
}