using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Catalog;

namespace ResQ.API.Data.Configurations.Catalog;

public class MerchantCategoryConfiguration : IEntityTypeConfiguration<MerchantCategory>
{
    public void Configure(EntityTypeBuilder<MerchantCategory> builder)
    {
        builder.ToTable("MerchantCategories");

        builder.HasKey(mc => new { mc.MerchantId, mc.CategoryId });

        builder.HasOne(mc => mc.Merchant)
            .WithMany(mp => mp.MerchantCategories)
            .HasForeignKey(mc => mc.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mc => mc.Category)
            .WithMany(c => c.MerchantCategories)
            .HasForeignKey(mc => mc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
