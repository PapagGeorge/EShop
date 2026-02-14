using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Ordering.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.HasIndex(o => o.UserId);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Street)
                .HasColumnName("ShippingAddress_Street")
                .IsRequired()
                .HasMaxLength(200);

            a.Property(p => p.City)
                .HasColumnName("ShippingAddress_City")
                .IsRequired()
                .HasMaxLength(100);

            a.Property(p => p.ZipCode)
                .HasColumnName("ShippingAddress_ZipCode")
                .IsRequired()
                .HasMaxLength(20);

            a.Property(p => p.Country)
                .HasColumnName("ShippingAddress_Country")
                .IsRequired()
                .HasMaxLength(100);
        });

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
    }
}
