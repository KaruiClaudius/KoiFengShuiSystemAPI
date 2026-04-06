using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.DataAccess.Models;

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
    public virtual DbSet<ListingImage> ListingImages { get; set; }
    public virtual DbSet<MarketCategory> MarketCategories { get; set; }
    public virtual DbSet<MarketplaceListing> MarketplaceListings { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<PostCategory> PostCategories { get; set; }
    public virtual DbSet<PostImage> PostImages { get; set; }
    public virtual DbSet<Recommendation> Recommendations { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<ShapeCategory> ShapeCategories { get; set; }
    public virtual DbSet<SubcriptionTier> SubcriptionTiers { get; set; }
    public virtual DbSet<TrafficLog> TrafficLogs { get; set; }
    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasOne(d => d.Element)
                .WithMany(p => p.Accounts)
                .HasForeignKey(d => d.ElementId);

            entity.HasOne(d => d.Role)
                .WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<FAQ>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.FAQs)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FengShuiDirection>(entity =>
        {
            entity.HasOne(d => d.Direction)
                .WithMany(p => p.FengShuiDirections)
                .HasForeignKey(d => d.DirectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Element)
                .WithMany(p => p.FengShuiDirections)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FishPond>(entity =>
        {
            entity.HasOne(d => d.DirectionPlacementNavigation)
                .WithMany(p => p.FishPonds)
                .HasForeignKey(d => d.DirectionPlacement)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Shape)
                .WithMany(p => p.FishPonds)
                .HasForeignKey(d => d.ShapeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.Follows)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Post)
                .WithMany(p => p.Follows)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KoiBreed>(entity =>
        {
            entity.HasOne(d => d.Country)
                .WithMany(p => p.KoiBreeds)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Element)
                .WithMany(p => p.KoiBreeds)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ListingImage>(entity =>
        {
            entity.HasOne(d => d.Image)
                .WithMany(p => p.ListingImages)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.MarketListing)
                .WithMany(p => p.ListingImages)
                .HasForeignKey(d => d.MarketListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceListing>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Element)
                .WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.ElementId);

            entity.HasOne(d => d.Tier)
                .WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.Posts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Element)
                .WithMany(p => p.Posts)
                .HasForeignKey(d => d.ElementId);

            entity.HasOne(d => d.IdNavigation)
                .WithMany(p => p.Posts)
                .HasForeignKey(d => d.Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PostImage>(entity =>
        {
            entity.HasOne(d => d.Image)
                .WithMany(p => p.PostImages)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Post)
                .WithMany(p => p.PostImages)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Breed)
                .WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.BreedId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Pond)
                .WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.PondId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShapeCategory>(entity =>
        {
            entity.HasOne(d => d.Element)
                .WithMany(p => p.ShapeCategories)
                .HasForeignKey(d => d.ElementId);
        });

        modelBuilder.Entity<TrafficLog>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.TrafficLogs)
                .HasForeignKey(d => d.AccountId);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasOne(d => d.Account)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Listing)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.ListingId);

            entity.HasOne(d => d.Tier)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.TierId);
        });
    }
}
