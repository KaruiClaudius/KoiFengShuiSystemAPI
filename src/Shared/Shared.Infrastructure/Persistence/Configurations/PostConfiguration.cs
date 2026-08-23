using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Element>()
            .WithMany(e => e.Posts)
            .HasForeignKey(d => d.ElementId);

        builder.HasOne(d => d.PostCategory)
            .WithMany(p => p.Posts)
            .HasForeignKey(d => d.PostCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
