using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.HasOne(d => d.Image)
            .WithMany(p => p.PostImages)
            .HasForeignKey(d => d.ImageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Post)
            .WithMany(p => p.PostImages)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
