using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BespokeDuaApi.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BespokeDuaDbContext>
{
    public BespokeDuaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=bespoke_dua;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BespokeDuaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BespokeDuaDbContext(options);
    }
}
