using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoiFengShuiSystem.Shared.Infrastructure.Persistence.Configurations;

public class TrafficLogConfiguration : IEntityTypeConfiguration<TrafficLog>
{
    public void Configure(EntityTypeBuilder<TrafficLog> builder)
    {
        builder.HasOne(d => d.Account)
            .WithMany(p => p.TrafficLogs)
            .HasForeignKey(d => d.AccountId);
    }
}
