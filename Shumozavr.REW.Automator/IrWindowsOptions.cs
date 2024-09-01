using System.ComponentModel;

namespace Shumozavr.REW.Automator;

public record IrWindowsOptions(
    [property:DefaultValue("Hann")]
    string RightWindowType,
    [property:DefaultValue("500")]
    string RightWindowWidthms);