using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;

namespace ResQ.API.Data.Configurations.Auth;

public class MerchantProfileConfiguration : IEntityTypeConfiguration<MerchantProfile>
{
    public void Configure(EntityTypeBuilder<MerchantProfile> builder)
    {
        builder.ToTable("MerchantProfiles");

        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.BusinessName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(mp => mp.Cuit)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(mp => mp.Address)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(mp => mp.ContactPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(mp => mp.Latitude)
            .IsRequired()
            .HasPrecision(10, 8);

        builder.Property(mp => mp.Longitude)
            .IsRequired()
            .HasPrecision(11, 8);

        builder.Property(mp => mp.MpConnectionStatus)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .HasDefaultValue(MpConnectionStatus.Disconnected);

        builder.HasIndex(mp => mp.UserId).IsUnique();
        builder.HasIndex(mp => mp.Cuit).IsUnique();

        builder.HasOne(mp => mp.User)
            .WithOne(u => u.MerchantProfile)
            .HasForeignKey<MerchantProfile>(mp => mp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
