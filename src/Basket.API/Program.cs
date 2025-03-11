﻿using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddBasicServiceDefaults();
builder.AddApplicationServices();

builder.Services.AddGrpc();

builder.Services.AddProblemDetails();

// OpenTelemetry Configuration
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

var meter = new Meter("Basket.API");
builder.Services.AddSingleton(meter);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Basket.API")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });

var app = builder.Build();

// Export Metric
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapDefaultEndpoints();

app.MapGrpcService<BasketService>();

app.Run();
