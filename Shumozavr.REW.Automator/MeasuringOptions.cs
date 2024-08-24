namespace Shumozavr.REW.Automator;

public record MeasuringOptions(
    int Angle,
    int Step,
    int Acceleration,
    string Title,
    string MeasurementLength,
    IrWindowsOptions IrWindowsOptions);