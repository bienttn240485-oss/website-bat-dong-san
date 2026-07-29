using RealEstateManagement.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class PropertyFurnitureItemConfiguration : IEntityTypeConfiguration<PropertyFurnitureItem>
{
    public void Configure(EntityTypeBuilder<PropertyFurnitureItem> builder)
    {
        builder.ToTable("PropertyFurnitureItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name).HasMaxLength(160).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(500);

        builder.HasIndex(item => item.PropertyId);
    }
}
