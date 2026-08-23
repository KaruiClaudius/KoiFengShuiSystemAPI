using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Post)
            .WithMany(p => p.Follows)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
