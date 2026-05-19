using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasOne(d => d.Account)
            .WithMany(p => p.Posts)
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Element>()
            .WithMany(p => p.Posts)
            .HasForeignKey(d => d.ElementId);

        builder.HasOne(d => d.IdNavigation)
            .WithMany(p => p.Posts)
            .HasForeignKey(d => d.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
