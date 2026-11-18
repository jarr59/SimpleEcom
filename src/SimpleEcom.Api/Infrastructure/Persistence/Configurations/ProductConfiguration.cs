using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleEcom.Api.Domain.Products;

namespace SimpleEcom.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();

        // Money es un objeto de valor: se guarda en columnas de la misma tabla.
        builder.ComplexProperty(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("Price").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasConversion<string>().HasMaxLength(3).IsRequired();
        });

        // Los eventos de dominio viven solo en memoria.
        builder.Ignore(p => p.DomainEvents);
    }
}
