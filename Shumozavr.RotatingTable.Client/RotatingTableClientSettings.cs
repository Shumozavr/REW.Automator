using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.Common.SerialPorts;

namespace Shumozavr.RotatingTable.Client;

public class RotatingTableClientSettings : IOptionsValue
{
    public static string OptionsKey => "RotatingTableClient";

    [ValidateObjectMembers] [Required] public SerialPortSettings SerialPort { get; set; } = default!;

    public TimeSpan CommandInitiationTimeout { get; set; } = TimeSpan.FromSeconds(10000);
}