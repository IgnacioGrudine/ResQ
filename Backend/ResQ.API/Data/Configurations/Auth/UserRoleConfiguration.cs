using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Auth;

namespace ResQ.API.Data.Configurations.Auth;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Role)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("smallint");

        builder.HasIndex(ur => new { ur.UserId, ur.Role }).IsUnique();

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
