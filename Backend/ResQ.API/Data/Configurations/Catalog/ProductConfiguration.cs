using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Catalog;

namespace ResQ.API.Data.Configurations.Catalog;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.ProductType)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("smallint");

        builder.Property(p => p.OriginalPrice)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.SalePrice)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.StockQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.PickupTimeStart).IsRequired();
        builder.Property(p => p.PickupTimeEnd).IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(p => p.Merchant)
            .WithMany(mp => mp.Products)
            .HasForeignKey(p => p.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
