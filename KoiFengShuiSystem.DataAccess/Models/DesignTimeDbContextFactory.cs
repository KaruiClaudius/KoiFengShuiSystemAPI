using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KoiFengShuiSystem.DataAccess.Models;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KoiFengShuiContext>
{
    public KoiFengShuiContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "KoiFengShuiSystem.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<KoiFengShuiContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new KoiFengShuiContext(optionsBuilder.Options);
    }
}
