using Microsoft.EntityFrameworkCore;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Data.Entities;

namespace OandaTrader.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<InstrumentSetting> InstrumentSettings => Set<InstrumentSetting>();
    public DbSet<Candle> Candles => Set<Candle>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<ModelVersion> ModelVersions => Set<ModelVersion>();
    public DbSet<CircuitBreakerEvent> CircuitBreakerEvents => Set<CircuitBreakerEvent>();
    public DbSet<BacktestRun> BacktestRuns => Set<BacktestRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Settings>(e =>
        {
            e.Property(s => s.Granularity).HasConversion<string>();
            e.HasData(new Settings { Id = 1 });
        });

        modelBuilder.Entity<InstrumentSetting>(e =>
        {
            e.HasIndex(i => i.Instrument).IsUnique();
            e.HasData(
                new InstrumentSetting { Id = 1, Instrument = "EUR_USD", Enabled = true },
                new InstrumentSetting { Id = 2, Instrument = "GBP_USD", Enabled = true },
                new InstrumentSetting { Id = 3, Instrument = "USD_JPY", Enabled = true },
                new InstrumentSetting { Id = 4, Instrument = "AUD_USD", Enabled = true });
        });

        modelBuilder.Entity<Candle>(e =>
        {
            e.Property(c => c.Granularity).HasConversion<string>();
            e.HasIndex(c => new { c.Instrument, c.Granularity, c.TimestampUtc }).IsUnique();
        });

        modelBuilder.Entity<Trade>(e =>
        {
            e.Property(t => t.Direction).HasConversion<string>();
            e.Property(t => t.Outcome).HasConversion<string>();
            e.Property(t => t.StrategySource).HasConversion<string>();
            e.HasIndex(t => t.Instrument);
            e.HasIndex(t => t.Outcome);
            e.HasIndex(t => t.StrategySource);
        });

        modelBuilder.Entity<BacktestRun>(e =>
        {
            e.Property(b => b.Granularity).HasConversion<string>();
        });
    }
}
