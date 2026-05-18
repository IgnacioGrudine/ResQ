using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.MercadoPago;

namespace ResQ.API.Data.Configurations.MercadoPago;

public class MerchantMpCredentialConfiguration : IEntityTypeConfiguration<MerchantMpCredential>
{
    public void Configure(EntityTypeBuilder<MerchantMpCredential> builder)
    {
        builder.ToTable("MerchantMpCredentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccessToken)
            .IsRequired()
            .HasMaxLength(600);

        builder.Property(c => c.RefreshToken)
            .IsRequired()
            .HasMaxLength(600);

        builder.Property(c => c.Scope)
            .HasMaxLength(200);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.AccessTokenExpiresAt).IsRequired();

        // UNIQUE: un comercio = una cuenta MP (1:1)
        builder.HasIndex(c => c.MerchantId).IsUnique();

        builder.HasOne(c => c.Merchant)
            .WithOne(mp => mp.MpCredential)
            .HasForeignKey<MerchantMpCredential>(c => c.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
