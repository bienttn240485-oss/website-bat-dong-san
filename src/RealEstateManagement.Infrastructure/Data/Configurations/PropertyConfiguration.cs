using RealEstateManagement.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.HasKey(property => property.Id);

        builder.Property(property => property.Code).HasMaxLength(50).IsRequired();
        builder.Property(property => property.Project).HasConversion<int?>();
        builder.Property(property => property.Area).HasMaxLength(120).IsRequired();
        builder.Property(property => property.Type).HasConversion<int>().IsRequired();
        builder.Property(property => property.Direction).HasMaxLength(80);
        builder.Property(property => property.LoanInfo).HasColumnType("TEXT");
        builder.Property(property => property.LegalStatus).HasMaxLength(200);
        builder.Property(property => property.FurniturePackage).HasMaxLength(200);
        builder.Property(property => property.Description).HasColumnType("TEXT");
        builder.Property(property => property.VideoUrl).HasMaxLength(500);
        builder.Property(property => property.Status).HasConversion<int>().IsRequired();
        builder.Property(property => property.Notes).HasColumnType("TEXT");
        builder.Property(property => property.CreatedAtUtc).IsRequired();
        builder.Property(property => property.UpdatedAtUtc).IsRequired();

        builder.HasIndex(property => property.Code).IsUnique();
        builder.HasIndex(property => property.Status);
        builder.HasIndex(property => property.Project);
        builder.HasIndex(property => property.Area);
        builder.HasIndex(property => property.Type);
        builder.HasIndex(property => property.AvailableFromDate);

        builder.HasMany(property => property.Images)
            .WithOne()
            .HasForeignKey(image => image.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(property => property.FurnitureItems)
            .WithOne()
            .HasForeignKey(item => item.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(property => property.Amenities)
            .WithOne()
            .HasForeignKey(amenity => amenity.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(property => property.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(property => property.FurnitureItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(property => property.Amenities).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
