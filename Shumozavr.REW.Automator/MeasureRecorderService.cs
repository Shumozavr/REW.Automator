using Microsoft.Extensions.Logging;
using Shumozavr.Common;
using Shumozavr.REW.Client.Http;
using Shumozavr.REW.Client.Http.Models.Measurement;
using Shumozavr.RotatingTable.Client;

namespace Shumozavr.REW.Automator;

public class MeasureRecorderService(
    RewMeasureClient measureClient,
    RewMeasurementClient measurementClient,
    IRotatingTableClient rotatingTableClient,
    ILogger<MeasureRecorderService> logger)
{
    private static readonly SemaphoreSlim MeasureLock = new(1, 1);
    private Task? _measureTask;
    private CancellationTokenSource? _measureCt;

    public async Task StopMeasureScenario(CancellationToken cancellationToken)
    {
        if (_measureTask == null || _measureTask.IsCompleted)
        {
            logger.LogInformation("Measurement was not started/finished, nothing to stop");
            return;
        }

        _measureCt?.Cancel();
        try
        {
            await _measureTask;
        }
        catch (OperationCanceledException)
        {
        }
        logger.LogInformation("Measurement was cancelled");
    }

    public async Task StartMeasureScenario(MeasuringOptions options, CancellationToken cancellationToken)
    {
        using var _ = await LockWrapper.LockOrThrow(MeasureLock);
        _measureCt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = _measureCt.Token;

        _measureTask = Task.Run(
            async () =>
            {
                logger.LogInformation("Starting to measure");
                logger.LogInformation("setting acceleration {acc}", options.Acceleration);
                await rotatingTableClient.SetAcceleration(options.Acceleration, cancellationToken);
                logger.LogInformation(
                    "starting measurement from 0 to {angle} with step {step}",
                    options.Angle,
                    options.Step);

                var angle = 0;
                while (angle < options.Angle)
                {
                    logger.LogInformation("current angle {angle}", angle);
                    await Measure(
                        angle,
                        options.Title,
                        options.MeasurementLength,
                        options.IrWindowsOptions,
                        cancellationToken);

                    var step = Math.Min(options.Step, options.Angle - angle);
                    await rotatingTableClient.Rotate(step, cancellationToken);

                    angle += step;
                }

                logger.LogInformation("current angle {angle}", angle);

                await Measure(
                    angle,
                    options.Title,
                    options.MeasurementLength,
                    options.IrWindowsOptions,
                    cancellationToken);
            },
            cancellationToken);
    }

    private async Task Measure(double currentAngle, string title, string length, IrWindowsOptions irWindowsOptions, CancellationToken cancellationToken)
    {
        logger.LogInformation("launching rew measure");
        await measureClient.Measure($"{title} {currentAngle:N1}", length, cancellationToken);
        var uuid = await measurementClient.GetSelectedMeasurementUuid(cancellationToken);
        var index = await measurementClient.GetSelectedMeasurementIndex(cancellationToken);
        logger.LogInformation("selected measurement uuid: {uuid}, index: {index}", uuid, index);
        logger.LogInformation("set offset time at ir start");
        await measurementClient.SetOffsetTimeAtIRStart(uuid, cancellationToken);
        await measurementClient.UpdateIrWindowsSettings(
            uuid,
            new UpdateIrWindowSettingsRequest(irWindowsOptions.RightWindowType, irWindowsOptions.RightWindowWidthms),
            cancellationToken);
    }
}