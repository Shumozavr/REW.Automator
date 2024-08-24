using System.ComponentModel.DataAnnotations;
using Shumozavr.Common;

namespace Shumozavr.RotatingTable.Common;

public class RotatingTableSettings : IOptionsValue
{
    public static string OptionsKey => "RotatingTable";

    public TimeSpan CommandInitiationTimeout { get; set; } = TimeSpan.FromSeconds(10000);

    [Required(AllowEmptyStrings = false)] public string SerialPort { get; set; } = default!;

    public TimeSpan ReadPortTimeout { get; set; } = TimeSpan.FromSeconds(10000);
    public TimeSpan WritePortTimeout { get; set; } = TimeSpan.FromSeconds(10000);
}