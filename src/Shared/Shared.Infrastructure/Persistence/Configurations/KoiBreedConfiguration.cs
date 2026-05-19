using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class KoiBreedConfiguration : IEntityTypeConfiguration<KoiBreed>
{
    public void Configure(EntityTypeBuilder<KoiBreed> builder)
    {
        builder.HasOne(d => d.Country)
            .WithMany(p => p.KoiBreeds)
            .HasForeignKey(d => d.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Element)
            .WithMany(p => p.KoiBreeds)
            .HasForeignKey(d => d.ElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
