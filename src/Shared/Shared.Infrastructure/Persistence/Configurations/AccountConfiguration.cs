using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasOne<Element>()
            .WithMany()
            .HasForeignKey(d => d.ElementId);

        builder.HasOne(d => d.Role)
            .WithMany()
            .HasForeignKey(d => d.RoleId);
    }
}
