using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Domain.Bookings;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Fields;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<FieldImage> FieldImages => Set<FieldImage>();
    public DbSet<FieldOperatingHour> FieldOperatingHours => Set<FieldOperatingHour>();
    public DbSet<FieldBlock> FieldBlocks => Set<FieldBlock>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<BookingServiceLine> BookingServices => Set<BookingServiceLine>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeUsage> PromoCodeUsages => Set<PromoCodeUsage>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<PropertyFurnitureItem> PropertyFurnitureItems => Set<PropertyFurnitureItem>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<LandlordContract> LandlordContracts => Set<LandlordContract>();
    public DbSet<TenantContract> TenantContracts => Set<TenantContract>();
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(user => user.DisplayName)
                .HasMaxLength(120);

            entity.Property(user => user.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(user => user.AccountStatus)
                .HasConversion<int>()
                .IsRequired();
        });
    }
}

