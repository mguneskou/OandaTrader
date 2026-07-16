using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OandaTrader.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BacktestRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Instrument = table.Column<string>(type: "TEXT", nullable: false),
                    Granularity = table.Column<string>(type: "TEXT", nullable: false),
                    ResultSummaryJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Candles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Instrument = table.Column<string>(type: "TEXT", nullable: false),
                    Granularity = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false),
                    Volume = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircuitBreakerEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    TriggeredLimit = table.Column<string>(type: "TEXT", nullable: false),
                    ResumedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircuitBreakerEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Instrument = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrainingSampleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MetricsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModelFilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RiskPercentPerTrade = table.Column<decimal>(type: "TEXT", nullable: false),
                    Granularity = table.Column<string>(type: "TEXT", nullable: false),
                    MaxDailyLossPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxConcurrentPositions = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxTradesPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    MlConfidenceThreshold = table.Column<decimal>(type: "TEXT", nullable: false),
                    RetrainAfterTradeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PausedReason = table.Column<string>(type: "TEXT", nullable: true),
                    PausedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OandaTradeId = table.Column<string>(type: "TEXT", nullable: true),
                    Instrument = table.Column<string>(type: "TEXT", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    EntryTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StopLoss = table.Column<decimal>(type: "TEXT", nullable: false),
                    TakeProfit = table.Column<decimal>(type: "TEXT", nullable: false),
                    Units = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExitPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    ExitTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PnL = table.Column<decimal>(type: "TEXT", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    StrategySource = table.Column<string>(type: "TEXT", nullable: false),
                    FeaturesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReasoningText = table.Column<string>(type: "TEXT", nullable: false),
                    MlConfidence = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "InstrumentSettings",
                columns: new[] { "Id", "Enabled", "Instrument" },
                values: new object[,]
                {
                    { 1, true, "EUR_USD" },
                    { 2, true, "GBP_USD" },
                    { 3, true, "USD_JPY" },
                    { 4, true, "AUD_USD" }
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "EngineEnabled", "Granularity", "MaxConcurrentPositions", "MaxDailyLossPercent", "MaxTradesPerDay", "MlConfidenceThreshold", "PausedAtUtc", "PausedReason", "RetrainAfterTradeCount", "RiskPercentPerTrade" },
                values: new object[] { 1, false, "M15", 3, 3.0m, 10, 0.55m, null, null, 25, 1.0m });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Instrument_Granularity_TimestampUtc",
                table: "Candles",
                columns: new[] { "Instrument", "Granularity", "TimestampUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSettings_Instrument",
                table: "InstrumentSettings",
                column: "Instrument",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Instrument",
                table: "Trades",
                column: "Instrument");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Outcome",
                table: "Trades",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_StrategySource",
                table: "Trades",
                column: "StrategySource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BacktestRuns");

            migrationBuilder.DropTable(
                name: "Candles");

            migrationBuilder.DropTable(
                name: "CircuitBreakerEvents");

            migrationBuilder.DropTable(
                name: "InstrumentSettings");

            migrationBuilder.DropTable(
                name: "ModelVersions");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
