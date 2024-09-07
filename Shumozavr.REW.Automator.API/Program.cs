using System.IO.Ports;
using System.Management;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using NLog.Web;
using Shumozavr.REW.Automator;
using Shumozavr.REW.Automator.API;
using Shumozavr.REW.Client;
using Shumozavr.REW.Client.Http;
using Shumozavr.RotatingTable.Client;
using Shumozavr.RotatingTable.Emulator;

var builder = WebApplication.CreateBuilder();
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", true, true);
// builder.Logging.AddJsonConsole(o =>
// {
//     o.JsonWriterOptions = o.JsonWriterOptions with { Indented = true };
// });
builder.Logging.ClearProviders().AddNLogWeb();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExtendedHttpClientLogging(
    o =>
    {
        o.LogBody = true;
        o.LogRequestStart = true;
        o.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None;
    });
builder.Services.AddRedaction(o => o.SetRedactor<NullRedactor>(DataClassification.None, DataClassification.Unknown));
builder.Services.AddHttpLogging(
    o =>
    {
        o.LoggingFields = HttpLoggingFields.All;
        o.CombineLogs = true;
    });

builder.Services.AddRewClient(c => c.GetSection(RewClientSettings.OptionsKey));
builder.Services.AddRotatingTable(builder.Configuration);
builder.Services.AddAutomator();
builder.Services.AddHostedService<InitService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpLogging();
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.MapPost(
    "/rew/measure/run",
    async (MeasuringOptions request, [FromServices]MeasureRecorderService recorder, CancellationToken cancellationToken) =>
    {
        await recorder.StartMeasureScenario(request, cancellationToken);
    })
    .WithTags("REW");

app.MapPost(
    "/rew/measure/stop",
    (
        [FromServices] MeasureRecorderService recorder,
        [FromQuery] bool softStop = true,
        CancellationToken cancellationToken = default) => recorder.StopMeasureScenario(cancellationToken))
    .WithTags("REW");

app.MapPost(
        "/rew/measure/subscribe",
        async ([FromBody]string message, [FromServices]RewMeasureClient client) =>
        {
            await client.Callback(new RewMessage(message, RewMessageSource.Measure), CancellationToken.None);
        })
    .WithTags("Сюда не смотри");

app.MapPost(
    "/rew/application/errors/subscribe",
    ([FromBody]dynamic message, [FromServices]RewApplicationClient client) =>
    {
        client.ErrorCallback(new DynamicRewMessage(message, RewMessageSource.ApplicationWarnings), CancellationToken.None);
    })
    .WithTags("Сюда не смотри");

app.MapPost(
    "/rew/application/warnings/subscribe",
    ([FromBody]dynamic message, [FromServices]RewApplicationClient client) =>
    {
        client.WarningCallback(new DynamicRewMessage(message, RewMessageSource.ApplicationWarnings), CancellationToken.None);
    })
    .WithTags("Сюда не смотри");

app.MapPost(
        "/rew/measurements/subscribe",
        async ([FromBody]dynamic message, [FromServices]RewMeasurementClient client) =>
        {
            await client.Callback(new DynamicRewMessage(message, RewMessageSource.Measurement), CancellationToken.None);
        })
    .WithTags("Сюда не смотри");


app.MapPost(
    "/rotatingTable/move/{angle:double}",
    async (double angle, [FromServices]IRotatingTableClient client) =>
    {
        await client.Rotate(angle, CancellationToken.None);
    })
    .WithTags("Rotating Table");

app.MapPost(
    "/rotatingTable/reset",
    async ([FromServices]IRotatingTableClient client) =>
    {
        await client.Reset(CancellationToken.None);
    })
    .WithTags("Rotating Table");

app.MapPost(
    "/rotatingTable/stop",
    async ([FromServices]IRotatingTableClient client) =>
    {
        await client.Stop(softStop: true, CancellationToken.None);
    })
    .WithTags("Rotating Table");

app.MapPost(
        "/rotatingTable/setAcceleration/{acceleration:int}",
        async (int acceleration, [FromServices]IRotatingTableClient client) =>
        {
            await client.SetAcceleration(acceleration, CancellationToken.None);
        })
    .WithTags("Rotating Table");

app.MapPost(
        "/rotatingTable/getAcceleration",
        async ([FromServices]IRotatingTableClient client) => 
        await client.GetAcceleration(CancellationToken.None))
    .WithTags("Rotating Table");


try
{
    using var mutex = EnsureOnlyOneInstance();
    app.Logger.LogInformation(((IConfigurationRoot)app.Configuration).GetDebugView());
    CheckComPort(app);

    await app.RunAsync();
}
catch (ApplicationException ex)
{
    app.Logger.LogError(ex.Message);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, ex.Message);
}
finally
{
    Console.WriteLine("Нажми любую клавишу чтобы выйти...");
    Console.ReadKey();
}

return;

void CheckComPort(WebApplication webApplication)
{
    var emulatorSettings = webApplication.Services.GetRequiredService<IOptions<RotatingTableEmulatorSettings>>();
    if (emulatorSettings.Value.Enabled)
    {
        return;
    }
    var tableSettings = webApplication.Services.GetRequiredService<IOptions<RotatingTableClientSettings>>();

    var portNames = SerialPort.GetPortNames();
    webApplication.Logger.LogInformation("Найденные COM порты:\n{ComPorts}", string.Join(Environment.NewLine, portNames));
    var usbDeviceInfos = GetUsbToComDevices(tableSettings.Value);
    webApplication.Logger.LogInformation("Найденные USB-COM устройства:\n{USBDevices}", string.Join(Environment.NewLine, usbDeviceInfos));


    var tablePort = tableSettings.Value.SerialPort.PortName;
    var isValidComPort = portNames.Contains(tablePort);
    var isValidUsbToComDevice = usbDeviceInfos.Select(x => x.Name).Any(x => x.Contains(tablePort));
    if (isValidComPort && isValidUsbToComDevice)
    {
        webApplication.Logger.LogInformation("{COMPort} распознан как поворотный стол", tablePort);
    }
    else
    {
        throw new
            ApplicationException(
                $"{tablePort} не распознан как поворотный стол. IsValidComPort: {isValidComPort}, IsValidUsbToComDevice: {isValidUsbToComDevice}");
    }

    return;

    static List<USBDeviceInfo> GetUsbToComDevices(RotatingTableClientSettings settings)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            throw new PlatformNotSupportedException("Must be ran on Windows");
        }
        var devices = new List<USBDeviceInfo>();

        using var searcher = new ManagementObjectSearcher(
            $@"Select * From Win32_PnPEntity WHERE Name LIKE '%{settings.SerialPort.ComPortDevice}%'");
        using var collection = searcher.Get();

        foreach (var device in collection)
        {
            var description = (string)device.GetPropertyValue("Name");

            devices.Add(new USBDeviceInfo(description));
        }
        return devices;
    }
}

static Mutex EnsureOnlyOneInstance()
{
    var mutex = new Mutex(true, "Shumozavr.REW.Automator", out var isMutexCreated);
    if (!isMutexCreated)
    {
        throw new ApplicationException("Может быть запущен только один инстанс программы");
    }

    return mutex;
}

record USBDeviceInfo(string Name);