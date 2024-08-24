using System.Text.Json;
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
builder.Services.AddHostedService<SubscriptionService>();

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
        await recorder.RunMeasureScenario(request, cancellationToken);
    });

app.MapPost(
        "/rew/measure/subscribe",
        async ([FromBody]string message, [FromServices]RewMeasureClient client) =>
        {
            await client.Callback(new RewMessage(message, RewMessageSource.Measure), CancellationToken.None);
        });

app.MapPost(
        "/rew/measurements/subscribe",
        async ([FromBody]dynamic message, [FromServices]RewMeasurementClient client) =>
        {
            await client.Callback(new DynamicRewMessage(message, RewMessageSource.Measurement), CancellationToken.None);
        });

app.MapPost(
    "/rotatingTable/stop/{stopType}",
    async ([FromServices]IRotatingTableClient client, string stopType = "soft", CancellationToken cancellationToken = default) =>
    {
        await (stopType == "soft"
            ? client.SoftStop(cancellationToken)
            : client.Stop(cancellationToken));
    });

await app.RunAsync();