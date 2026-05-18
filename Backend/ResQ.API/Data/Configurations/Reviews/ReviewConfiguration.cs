using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Reviews;

namespace ResQ.API.Data.Configurations.Reviews;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired()
            .HasColumnType("smallint");

        builder.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5"));

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        // UNIQUE: una sola reseña por orden (1:1)
        builder.HasIndex(r => r.OrderId).IsUnique();

        builder.HasOne(r => r.Order)
            .WithOne(o => o.Review)
            .HasForeignKey<Review>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Merchant)
            .WithMany(mp => mp.Reviews)
            .HasForeignKey(r => r.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
