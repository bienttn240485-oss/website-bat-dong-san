using RealEstateManagement.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.ToTable("PropertyImages");
        builder.HasKey(image => image.Id);

        builder.Property(image => image.Url).HasMaxLength(500).IsRequired();
        builder.Property(image => image.AltText).HasMaxLength(200);
        builder.Property(image => image.SortOrder).IsRequired();
        builder.Property(image => image.IsPrimary).IsRequired();

        builder.HasIndex(image => image.PropertyId);
    }
}
