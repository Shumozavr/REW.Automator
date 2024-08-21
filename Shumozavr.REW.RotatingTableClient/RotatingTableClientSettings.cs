using System.ComponentModel.DataAnnotations;
using Shumozavr.Common;

namespace Shumozavr.REW.RotatingTableClient;

public class RotatingTableClientSettings : IOptionsValue
{
    public static string OptionsKey => "RotatingTableClient";

    public TimeSpan CommandInitiationTimeout { get; set; } = TimeSpan.FromSeconds(1);

    [Required(AllowEmptyStrings = false)] public string SerialPort { get; set; } = default!;

    public TimeSpan ReadPortTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan WritePortTimeout { get; set; } = TimeSpan.FromSeconds(1);
}