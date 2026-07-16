using Microsoft.EntityFrameworkCore;

namespace OandaTrader.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
