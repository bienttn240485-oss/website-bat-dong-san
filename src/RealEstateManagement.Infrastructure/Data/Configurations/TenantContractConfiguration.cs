using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class TenantContractConfiguration : IEntityTypeConfiguration<TenantContract>
{
    public void Configure(EntityTypeBuilder<TenantContract> builder)
    {
        builder.ToTable("TenantContracts");
        builder.HasKey(contract => contract.Id);

        builder.Property(contract => contract.TenantName).HasMaxLength(160).IsRequired();
        builder.Property(contract => contract.ManagerName).HasMaxLength(160);
        builder.Property(contract => contract.RentalPrice).IsRequired();
        builder.Property(contract => contract.SignedDate).IsRequired();
        builder.Property(contract => contract.TermMonths).IsRequired();
        builder.Property(contract => contract.ExpiryDate).IsRequired();
        builder.Property(contract => contract.DepositAmount).IsRequired();
        builder.Property(contract => contract.PeCode).HasMaxLength(80);
        builder.Property(contract => contract.PassCode).HasMaxLength(80);
        builder.Property(contract => contract.Status).HasConversion<int>().IsRequired();
        builder.Property(contract => contract.Notes).HasColumnType("TEXT");
        builder.Property(contract => contract.CreatedAtUtc).IsRequired();
        builder.Property(contract => contract.UpdatedAtUtc).IsRequired();

        builder.HasIndex(contract => contract.PropertyId);
        builder.HasIndex(contract => contract.Status);
        builder.HasIndex(contract => contract.SignedDate);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(contract => contract.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
