using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning();
builder.AddDefaultOpenApi(withApiVersioning);

// 🔹 Configuração do OpenTelemetry
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

// 🔹 Criando e registrando o Meter no DI container
var meter = new Meter("Ordering.API");
builder.Services.AddSingleton(meter);

//var orderPlacedCounter = meter.CreateCounter<long>("order_placed_total", description: "Total number of orders.");
//var orderFailedCounter = meter.CreateCounter<long>("order_failed_total", description: "Number of failed order attempts.");
//var orderProcessingTime = meter.CreateHistogram<double>("order_processing_duration_ms", "ms", "Time taken to process an order.");
//var orderErrorRate = meter.CreateHistogram<double>("order_error_rate_percent", "percent", "Percentage of orders that encountered an error.");

//builder.Services.AddSingleton(meter);
//builder.Services.AddSingleton(orderPlacedCounter);
//builder.Services.AddSingleton(orderFailedCounter);
//builder.Services.AddSingleton(orderProcessingTime);
//builder.Services.AddSingleton(orderErrorRate);

// Configurando métricas OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Ordering.API")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });

var app = builder.Build();

//orderPlacedCounter.Add(0); // Ensure it's registered with an initial value

//Console.WriteLine("✅ Inicializando order_placed_count");
//Console.WriteLine(orderPlacedCounter);

// Exposição do endpoint de métricas para Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");
orders.MapOrdersApiV1().RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
