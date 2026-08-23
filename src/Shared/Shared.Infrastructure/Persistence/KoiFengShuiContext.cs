using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence;

public class KoiFengShuiContext : DbContext
{
    public KoiFengShuiContext()
    {
    }

    public KoiFengShuiContext(DbContextOptions<KoiFengShuiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<Country> Countries { get; set; }
    public virtual DbSet<Direction> Directions { get; set; }
    public virtual DbSet<Element> Elements { get; set; }
    public virtual DbSet<FAQ> FAQs { get; set; }
    public virtual DbSet<FengShuiDirection> FengShuiDirections { get; set; }
    public virtual DbSet<FishPond> FishPonds { get; set; }
    public virtual DbSet<Follow> Follows { get; set; }
    public virtual DbSet<Image> Images { get; set; }
    public virtual DbSet<KoiBreed> KoiBreeds { get; set; }
    public virtual DbSet<PartnerShop> PartnerShops { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<PostCategory> PostCategories { get; set; }
    public virtual DbSet<PostImage> PostImages { get; set; }
    public virtual DbSet<Recommendation> Recommendations { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<ShapeCategory> ShapeCategories { get; set; }
    public virtual DbSet<TrafficLog> TrafficLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KoiFengShuiContext).Assembly);
    }

    /// <summary>
    /// PostgreSQL DateTime mapping strategy: every DateTime property maps to
    /// 'timestamp without time zone'.
    ///
    /// Why not the Npgsql default ('timestamp with time zone'): since Npgsql 6,
    /// timestamptz only accepts DateTime values whose Kind is UTC and throws
    /// "Cannot write DateTime with Kind=Local" otherwise. This codebase predates that
    /// rule and mixes DateTime.Now, DateTime.UtcNow and Unspecified kinds across many
    /// write paths historically, now standardized to UtcNow, so adopting timestamptz remains possible later
    /// would require auditing every producer first.
    ///
    /// Tradeoff accepted: 'timestamp without time zone' stores wall-clock values
    /// verbatim with no timezone conversion or DST awareness; correctness relies on
    /// the convention that writers use UtcNow. If the codebase is later normalized to
    /// produce UTC-only DateTimes, this override is the single place to flip to
    /// timestamptz.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");
    }
}
