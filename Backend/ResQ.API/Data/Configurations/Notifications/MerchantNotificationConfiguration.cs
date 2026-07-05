using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Notifications;

namespace ResQ.API.Data.Configurations.Notifications;

public class MerchantNotificationConfiguration : IEntityTypeConfiguration<MerchantNotification>
{
    public void Configure(EntityTypeBuilder<MerchantNotification> builder)
    {
        builder.ToTable("MerchantNotifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("smallint");

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.IsRead)
            .IsRequired();

        // Most queries fetch a merchant's notifications ordered by recency, so index the FK.
        builder.HasIndex(n => n.MerchantId);

        builder.HasOne(n => n.Merchant)
            .WithMany()
            .HasForeignKey(n => n.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sin ON DELETE CASCADE desde Order — el historial financiero nunca se borra en cascada.
        builder.HasOne(n => n.Order)
            .WithMany()
            .HasForeignKey(n => n.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
