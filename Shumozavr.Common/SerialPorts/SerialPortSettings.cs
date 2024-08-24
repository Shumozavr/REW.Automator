using System.ComponentModel.DataAnnotations;

namespace Shumozavr.Common.SerialPorts;

public class SerialPortSettings : IOptionsValue
{
    public static string OptionsKey => "SerialPort";

    [Required(AllowEmptyStrings = false)] public string PortName { get; set; } = default!;

    public TimeSpan ReadPortTimeout { get; set; } = TimeSpan.FromSeconds(10000);
    public TimeSpan WritePortTimeout { get; set; } = TimeSpan.FromSeconds(10000);
}