using RealEstateManagement.Domain.Fields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class FieldImageConfiguration : IEntityTypeConfiguration<FieldImage>
{
    public void Configure(EntityTypeBuilder<FieldImage> builder)
    {
        builder.ToTable("FieldImages");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(image => image.AltText).HasMaxLength(200);
        builder.HasIndex(image => new { image.FieldId, image.SortOrder });
    }
}

