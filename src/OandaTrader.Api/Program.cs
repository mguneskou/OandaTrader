using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

builder.Services.AddScoped<CandleCacheService>();
builder.Services.AddScoped<BacktestRunner>();

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

app.Run();
