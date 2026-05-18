using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.API.Models.Orders;

namespace ResQ.API.Data.Configurations.Orders;

public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("OrderDetails");

        builder.HasKey(od => od.Id);

        builder.Property(od => od.Quantity).IsRequired();

        builder.Property(od => od.UnitPrice)
            .IsRequired()
            .HasPrecision(10, 2);

        // Cascade: si se borra la orden, se borran sus detalles
        builder.HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: el detalle sobrevive aunque el producto sea eliminado
        builder.HasOne(od => od.Product)
            .WithMany(p => p.OrderDetails)
            .HasForeignKey(od => od.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
