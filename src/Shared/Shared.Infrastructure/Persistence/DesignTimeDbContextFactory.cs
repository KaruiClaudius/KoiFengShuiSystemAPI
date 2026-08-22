using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef` when adding or scripting migrations for
/// <see cref="KoiFengShuiContext"/>. It lives in the same assembly as the context so
/// the migrations assembly stays self-contained under src/.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KoiFengShuiContext>
{
    /// <summary>
    /// Fallback connection string mirroring the docker-compose postgres service
    /// defaults; used because migration AUTHORING never connects to a database.
    /// For applying migrations, pass the real target explicitly:
    /// `dotnet ef database update --connection "&lt;connection-string&gt;"`.
    /// </summary>
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=koi_fengshui;Username=koi;Password=koi_dev_pw";

    public KoiFengShuiContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KoiFengShuiContext>();
        optionsBuilder.UseNpgsql(
            DesignTimeConnectionString,
            b => b.MigrationsAssembly(typeof(KoiFengShuiContext).Assembly.GetName().Name));

        return new KoiFengShuiContext(optionsBuilder.Options);
    }
}
