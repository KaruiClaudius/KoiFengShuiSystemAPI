using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.HasOne(d => d.Account)
            .WithMany()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Breed)
            .WithMany(p => p.Recommendations)
            .HasForeignKey(d => d.BreedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Pond)
            .WithMany(p => p.Recommendations)
            .HasForeignKey(d => d.PondId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
