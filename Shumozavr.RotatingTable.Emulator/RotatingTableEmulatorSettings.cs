﻿using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.Common.SerialPorts;

namespace Shumozavr.RotatingTable.Emulator;

public class RotatingTableEmulatorSettings : IOptionsValue
{
    public static string OptionsKey => "RotatingTableEmulator";

    [ValidateObjectMembers] [Required] public SerialPortSettings SerialPort { get; set; } = default!;
}