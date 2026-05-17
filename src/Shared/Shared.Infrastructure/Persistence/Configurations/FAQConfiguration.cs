using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class FAQConfiguration : IEntityTypeConfiguration<FAQ>
{
    public void Configure(EntityTypeBuilder<FAQ> builder)
    {
        builder.HasOne(d => d.Account)
            .WithMany(p => p.FAQs)
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
