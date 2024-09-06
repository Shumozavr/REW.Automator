using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Diagnostics;
using NLog.Web;
using Shumozavr.REW.Automator;
using Shumozavr.REW.Automator.API;
using Shumozavr.REW.Client;
using Shumozavr.REW.Client.Http;
using Shumozavr.RotatingTable.Client;
using Shumozavr.RotatingTable.Emulator;

var builder = WebApplication.CreateBuilder(args);
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

await app.RunAsync();