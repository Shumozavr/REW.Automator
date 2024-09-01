using System.ComponentModel;

namespace Shumozavr.REW.Automator;

public record MeasuringOptions(
    [property:DefaultValue(10)]
    int Angle,
    [property:DefaultValue(1)]
    int Step,
    [property:DefaultValue(1)]
    int Acceleration,
    [property:DefaultValue("test")]
    string Title,
    [property:DefaultValue("256k")]
    string MeasurementLength,
    IrWindowsOptions IrWindowsOptions);