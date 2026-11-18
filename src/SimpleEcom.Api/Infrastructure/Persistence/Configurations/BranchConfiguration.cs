using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleEcom.Api.Domain.Branches;

namespace SimpleEcom.Api.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, value => new BranchId(value))
            .ValueGeneratedNever();

        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(b => b.Code).IsUnique();

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(30);
        builder.Property(b => b.AllowsPickup).IsRequired();
        builder.Property(b => b.AllowsDelivery).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();

        // Los eventos de dominio viven solo en memoria.
        builder.Ignore(b => b.DomainEvents);
    }
}
