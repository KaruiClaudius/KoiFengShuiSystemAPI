using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class FishPondConfiguration : IEntityTypeConfiguration<FishPond>
{
    public void Configure(EntityTypeBuilder<FishPond> builder)
    {
        builder.HasOne(d => d.DirectionPlacementNavigation)
            .WithMany(p => p.FishPonds)
            .HasForeignKey(d => d.DirectionPlacement)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Shape)
            .WithMany(p => p.FishPonds)
            .HasForeignKey(d => d.ShapeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
