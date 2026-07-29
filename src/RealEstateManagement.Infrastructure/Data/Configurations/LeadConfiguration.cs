using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");
        builder.HasKey(lead => lead.Id);

        builder.Property(lead => lead.Name).HasMaxLength(160).IsRequired();
        builder.Property(lead => lead.Contact).HasMaxLength(160).IsRequired();
        builder.Property(lead => lead.Subject).HasMaxLength(200);
        builder.Property(lead => lead.Message).HasColumnType("TEXT");
        builder.Property(lead => lead.Language).HasMaxLength(20).IsRequired();
        builder.Property(lead => lead.Status).HasConversion<int>().IsRequired();
        builder.Property(lead => lead.CreatedAtUtc).IsRequired();
        builder.Property(lead => lead.UpdatedAtUtc).IsRequired();

        builder.HasIndex(lead => lead.Status);
        builder.HasIndex(lead => lead.CreatedAtUtc);
        builder.HasIndex(lead => lead.PropertyId);
        builder.HasIndex(lead => lead.AssignedToUserId);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(lead => lead.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(lead => lead.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
