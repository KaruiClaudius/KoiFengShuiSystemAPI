using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class PartnerShopConfiguration : IEntityTypeConfiguration<PartnerShop>
{
    public void Configure(EntityTypeBuilder<PartnerShop> builder)
    {
        builder.ToTable("PartnerShops");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.LinkUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Note)
            .HasMaxLength(1000);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.HasIndex(s => s.IsActive);
    }
}
