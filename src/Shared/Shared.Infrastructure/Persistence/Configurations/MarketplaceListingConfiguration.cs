using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
{
    public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
    {
        builder.HasOne(d => d.Account)
            .WithMany(p => p.MarketplaceListings)
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Category)
            .WithMany(p => p.MarketplaceListings)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Element>()
            .WithMany(p => p.MarketplaceListings)
            .HasForeignKey(d => d.ElementId);

        builder.HasOne(d => d.Tier)
            .WithMany(p => p.MarketplaceListings)
            .HasForeignKey(d => d.TierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
