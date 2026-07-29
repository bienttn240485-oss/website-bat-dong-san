using RealEstateManagement.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("PropertyAmenities");
        builder.HasKey(amenity => amenity.Id);

        builder.Property(amenity => amenity.Name).HasMaxLength(160).IsRequired();

        builder.HasIndex(amenity => amenity.PropertyId);
    }
}
