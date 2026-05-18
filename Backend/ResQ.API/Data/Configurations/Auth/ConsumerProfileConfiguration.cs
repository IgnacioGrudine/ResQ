using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Auth;

namespace ResQ.API.Data.Configurations.Auth;

public class ConsumerProfileConfiguration : IEntityTypeConfiguration<ConsumerProfile>
{
    public void Configure(EntityTypeBuilder<ConsumerProfile> builder)
    {
        builder.ToTable("ConsumerProfiles");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.PhoneNumber)
            .HasMaxLength(20);

        builder.HasIndex(cp => cp.UserId).IsUnique();

        builder.HasOne(cp => cp.User)
            .WithOne(u => u.ConsumerProfile)
            .HasForeignKey<ConsumerProfile>(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
