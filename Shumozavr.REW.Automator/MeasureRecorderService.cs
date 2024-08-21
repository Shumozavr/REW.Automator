using Shumozavr.REW.Client.Http;
using Shumozavr.REW.RotatingTableClient;

namespace Shumozavr.REW.Automator;

public class MeasureRecorderService(
    RewMeasureClient measureClient,
    RewMeasurementClient measurementClient,
    IRotatingTableClient rotatingTableClient)
{
    public async Task Measure(MeasuringOptions options, CancellationToken cancellationToken)
    {
        await rotatingTableClient.SetAcceleration(options.Acceleration, cancellationToken);
        for (var angle = 0; angle < options.Angle; angle+=options.Step)
        {
            var id = await measureClient.Measure(options.Title, cancellationToken);
            await measurementClient.SetOffsetTimeAtIRStart(id, cancellationToken);
            await rotatingTableClient.Rotate(angle, cancellationToken);
        }
    }
}