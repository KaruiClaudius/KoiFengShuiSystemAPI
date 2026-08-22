using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class TrafficLogConfiguration : IEntityTypeConfiguration<TrafficLog>
{
    public void Configure(EntityTypeBuilder<TrafficLog> builder)
    {
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(d => d.AccountId);
    }
}
