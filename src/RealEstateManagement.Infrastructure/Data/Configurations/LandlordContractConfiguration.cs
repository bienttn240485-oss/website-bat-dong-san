using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealEstateManagement.Infrastructure.Data.Configurations;

public sealed class LandlordContractConfiguration : IEntityTypeConfiguration<LandlordContract>
{
    public void Configure(EntityTypeBuilder<LandlordContract> builder)
    {
        builder.ToTable("LandlordContracts");
        builder.HasKey(contract => contract.Id);

        builder.Property(contract => contract.LandlordName).HasMaxLength(160).IsRequired();
        builder.Property(contract => contract.PeCode).HasMaxLength(80);
        builder.Property(contract => contract.SaleName).HasMaxLength(160);
        builder.Property(contract => contract.InputPrice).IsRequired();
        builder.Property(contract => contract.SignedDate).IsRequired();
        builder.Property(contract => contract.ExpiryDate).IsRequired();
        builder.Property(contract => contract.DepositStatus).HasConversion<int>().IsRequired();
        builder.Property(contract => contract.PaymentWindow).HasMaxLength(120);
        builder.Property(contract => contract.Notes).HasColumnType("TEXT");
        builder.Property(contract => contract.CreatedAtUtc).IsRequired();
        builder.Property(contract => contract.UpdatedAtUtc).IsRequired();

        builder.HasIndex(contract => contract.PropertyId).IsUnique();

        builder.HasOne<Property>()
            .WithOne()
            .HasForeignKey<LandlordContract>(contract => contract.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
