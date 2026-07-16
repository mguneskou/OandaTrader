using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OandaTrader.Api;
using OandaTrader.Api.Hubs;
using OandaTrader.Api.Realtime;
using OandaTrader.Domain.Strategies;
using OandaTrader.Infrastructure.Backtesting;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Ml;
using OandaTrader.Infrastructure.Oanda;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDb")));

builder.Services.Configure<OandaOptions>(builder.Configuration.GetSection(OandaOptions.SectionName));
builder.Services.AddHttpClient<OandaRestClient>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<OandaOptions>>().Value;
    http.BaseAddress = options.RestBaseUri;
    if (!string.IsNullOrWhiteSpace(options.ApiToken))
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
    }
});

// Separate long-lived HttpClient for the pricing stream: no total timeout (the connection
// stays open indefinitely), unlike the REST client's default per-request timeout.
builder.Services.AddHttpClient(OandaStreamingClient.HttpClientName, (sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<OandaOptions>>().Value;
    http.BaseAddress = options.StreamingBaseUri;
    http.Timeout = Timeout.InfiniteTimeSpan;
    if (!string.IsNullOrWhiteSpace(options.ApiToken))
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
    }
});
builder.Services.AddSingleton<OandaStreamingClient>();

builder.Services.AddScoped<CandleCacheService>();
builder.Services.AddScoped<BacktestRunner>();

builder.Services.AddSingleton<EngineStateCache>();
builder.Services.AddSingleton<EngineBroadcaster>();

builder.Services.Configure<MlOptions>(builder.Configuration.GetSection(MlOptions.SectionName));
builder.Services.PostConfigure<MlOptions>(o =>
{
    // Resolve a relative models directory against the content root so it doesn't depend on
    // the process working directory (VS2022 and `dotnet run` can differ).
    if (!Path.IsPathRooted(o.ModelsDirectory))
    {
        o.ModelsDirectory = Path.Combine(builder.Environment.ContentRootPath, o.ModelsDirectory);
    }
});
builder.Services.AddScoped<ModelTrainingService>();
builder.Services.AddSingleton<MlPredictionService>();
builder.Services.AddSingleton<IWinProbabilityPredictor>(sp => sp.GetRequiredService<MlPredictionService>());

builder.Services.AddHostedService<TradingEngineHostedService>();
builder.Services.AddHostedService<PriceStreamingHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Skipped in Development: the SPA dev proxy talks to the plain-http listener, and
// redirecting that to https here would bounce the browser's fetch to a different
// origin (https on a different port), which gets blocked by CORS.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();
app.MapHub<EngineHub>("/hubs/engine");

app.Run();
