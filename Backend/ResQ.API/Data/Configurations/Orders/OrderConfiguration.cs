using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;

namespace ResQ.API.Data.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(o => o.PlatformFee)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(o => o.MerchantEarnings)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(o => o.ExternalReference)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(o => o.MpPreferenceId)
            .HasMaxLength(255);

        builder.Property(o => o.OrderStatus)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("smallint");

        builder.Property(o => o.PickupCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(o => o.ExternalReference).IsUnique();
        builder.HasIndex(o => o.PickupCode).IsUnique();

        // Sin ON DELETE CASCADE — historial financiero no se borra
        builder.HasOne(o => o.Consumer)
            .WithMany(cp => cp.Orders)
            .HasForeignKey(o => o.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Merchant)
            .WithMany(mp => mp.Orders)
            .HasForeignKey(o => o.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
