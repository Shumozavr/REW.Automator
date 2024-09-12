using System.ComponentModel.DataAnnotations;

namespace Shumozavr.Common.SerialPorts;

public class SerialPortSettings : IOptionsValue
{
    public static string OptionsKey => "SerialPort";

    [Required(AllowEmptyStrings = false)] public string PortName { get; set; } = default!;
    public string ComPortDevice { get; set; } = "Silicon Labs CP210x USB to UART Bridge";

    public int ReconnectRetryCount { get; set; } = 3;
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(3);

    public TimeSpan WritePortTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public int BaudRate { get; set; } = 115200;
}