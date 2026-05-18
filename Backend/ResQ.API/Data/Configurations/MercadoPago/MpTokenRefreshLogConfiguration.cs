using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.MercadoPago;

namespace ResQ.API.Data.Configurations.MercadoPago;

public class MpTokenRefreshLogConfiguration : IEntityTypeConfiguration<MpTokenRefreshLog>
{
    public void Configure(EntityTypeBuilder<MpTokenRefreshLog> builder)
    {
        builder.ToTable("MpTokenRefreshLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Success).IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(500);

        builder.HasOne(l => l.Merchant)
            .WithMany(mp => mp.MpTokenRefreshLogs)
            .HasForeignKey(l => l.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
