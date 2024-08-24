using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.REW.Automator;
using Shumozavr.REW.Automator.API;
using Shumozavr.REW.Client;
using Shumozavr.REW.Client.Http;
using Shumozavr.RotatingTable.Client;
using Shumozavr.RotatingTable.Common;
using ServiceCollectionExtensions = Shumozavr.RotatingTable.Client.ServiceCollectionExtensions;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExtendedHttpClientLogging(
    o =>
    {
        o.LogBody = true;
        o.LogRequestStart = true;
    });
builder.Services.AddRedaction(o => o.SetRedactor<NullRedactor>());
builder.Services.AddHttpLogging(
    o =>
    {
        o.LoggingFields = HttpLoggingFields.All;
    });

builder.Services.AddRewClient(c => c.GetSection(RewClientSettings.OptionsKey));
builder.Services.AddRotatingTableClient(c => c.GetSection(RotatingTableClientSettings.OptionsKey));
builder.Services.AddHostedService<SubscriptionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpLogging();
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.MapPost(
    "/rew/measure/run",
    async (MeasureRecorderService recorder, CancellationToken cancellationToken) =>
    {
        await recorder.Measure(new MeasuringOptions(100, 10, 5, "Mah title"), cancellationToken);
    });

app.MapPost(
        "/rew/measure/subscribe",
        async (string message, RewMeasureClient client) =>
        {
            await client.Callback(new RewMessage(message, RewMessageSource.Measure), CancellationToken.None);
        })
   .WithGroupName("REW.Measure")
   .WithName("SubscribeMeasureCallback")
   .WithOpenApi();

app.MapPost(
        "/rew/measurements/subscribe",
        async (string message, RewMeasurementClient client) =>
        {
            await client.Callback(new RewMessage(message, RewMessageSource.Measurement), CancellationToken.None);
        })
   .WithGroupName("REW.Measurements")
   .WithName("SubscribeMeasurementCallback")
   .WithOpenApi();

app.MapPost(
    "/rotatingTable/stop/{stopType}",
    async (IRotatingTableClient client, string stopType = "soft", CancellationToken cancellationToken = default) =>
    {
        await (stopType == "soft"
            ? client.SoftStop(cancellationToken)
            : client.Stop(cancellationToken));
    })
   .WithGroupName("RotatingTable")
   .WithName("Stop")
   .WithOpenApi();

await app.RunAsync();