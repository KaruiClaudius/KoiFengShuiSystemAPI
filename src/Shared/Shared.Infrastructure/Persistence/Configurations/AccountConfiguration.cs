using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasOne<Element>()
            .WithMany(p => p.Accounts)
            .HasForeignKey(d => d.ElementId);

        builder.HasOne(d => d.Role)
            .WithMany(p => p.Accounts)
            .HasForeignKey(d => d.RoleId);
    }
}
