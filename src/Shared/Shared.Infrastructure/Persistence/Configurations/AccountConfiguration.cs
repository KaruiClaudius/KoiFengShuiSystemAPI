using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasOne(d => d.Element)
            .WithMany(p => p.Accounts)
            .HasForeignKey(d => d.ElementId);

        builder.HasOne(d => d.Role)
            .WithMany(p => p.Accounts)
            .HasForeignKey(d => d.RoleId);
    }
}
