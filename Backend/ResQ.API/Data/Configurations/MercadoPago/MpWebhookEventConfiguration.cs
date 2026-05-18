using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;

namespace ResQ.API.Data.Configurations.MercadoPago;

public class MpWebhookEventConfiguration : IEntityTypeConfiguration<MpWebhookEvent>
{
    public void Configure(EntityTypeBuilder<MpWebhookEvent> builder)
    {
        builder.ToTable("MpWebhookEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Topic)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.RawPayload)
            .HasColumnType("text");

        builder.Property(e => e.ProcessingStatus)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .HasDefaultValue(WebhookProcessingStatus.Pending);

        builder.Property(e => e.AttemptCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasDefaultValue((byte)0);

        builder.Property(e => e.LastErrorMessage)
            .HasMaxLength(500);

        // UNIQUE sobre MpNotificationId — implementa idempotencia
        builder.HasIndex(e => e.MpNotificationId).IsUnique();
    }
}
