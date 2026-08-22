using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Listing)
            .WithMany(p => p.Transactions)
            .HasForeignKey(d => d.ListingId);

        builder.HasOne(d => d.Tier)
            .WithMany(p => p.Transactions)
            .HasForeignKey(d => d.TierId);
    }
}
