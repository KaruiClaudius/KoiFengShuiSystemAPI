using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class ShapeCategoryConfiguration : IEntityTypeConfiguration<ShapeCategory>
{
    public void Configure(EntityTypeBuilder<ShapeCategory> builder)
    {
        builder.HasOne(d => d.Element)
            .WithMany(p => p.ShapeCategories)
            .HasForeignKey(d => d.ElementId);
    }
}
