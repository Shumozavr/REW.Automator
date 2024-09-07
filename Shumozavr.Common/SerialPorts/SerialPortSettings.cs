using System.ComponentModel.DataAnnotations;

namespace Shumozavr.Common.SerialPorts;

public class SerialPortSettings : IOptionsValue
{
    public static string OptionsKey => "SerialPort";

    [Required(AllowEmptyStrings = false)] public string PortName { get; set; } = default!;
    public string ComPortDevice { get; set; } = "Silicon Labs CP210x USB to UART Bridge";

    public TimeSpan WritePortTimeout { get; set; } = TimeSpan.FromSeconds(2);
}