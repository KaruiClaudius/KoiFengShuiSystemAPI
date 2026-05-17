using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class ListingImageConfiguration : IEntityTypeConfiguration<ListingImage>
{
    public void Configure(EntityTypeBuilder<ListingImage> builder)
    {
        builder.HasOne(d => d.Image)
            .WithMany(p => p.ListingImages)
            .HasForeignKey(d => d.ImageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.MarketListing)
            .WithMany(p => p.ListingImages)
            .HasForeignKey(d => d.MarketListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
