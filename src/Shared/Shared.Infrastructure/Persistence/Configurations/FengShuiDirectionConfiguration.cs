using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class FengShuiDirectionConfiguration : IEntityTypeConfiguration<FengShuiDirection>
{
    public void Configure(EntityTypeBuilder<FengShuiDirection> builder)
    {
        builder.HasOne(d => d.Direction)
            .WithMany(p => p.FengShuiDirections)
            .HasForeignKey(d => d.DirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Element)
            .WithMany(p => p.FengShuiDirections)
            .HasForeignKey(d => d.ElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
