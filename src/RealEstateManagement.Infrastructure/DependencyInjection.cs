using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Application.Fields;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Infrastructure.Bookings;
using RealEstateManagement.Infrastructure.Contracts;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Dashboard;
using RealEstateManagement.Infrastructure.Fields;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.Leads;
using RealEstateManagement.Infrastructure.Properties;
using RealEstateManagement.Infrastructure.Reports;
using RealEstateManagement.Infrastructure.SeedData;
using RealEstateManagement.Infrastructure.Time;
using RealEstateManagement.Application.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RealEstateManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string? contentRootPath = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=App_Data/real-estate-management.db";
        connectionString = ResolveSqliteConnectionString(connectionString, contentRootPath);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DevelopmentOwnerOptions>(configuration.GetSection("SeedOwner"));
        services.Configure<DevelopmentInternalUsersOptions>(configuration.GetSection("SeedInternalUsers"));
        services.AddScoped<IdentitySeeder>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddScoped<IFieldStore, EfFieldStore>();
        services.AddScoped<IFieldService, FieldService>();
        services.AddScoped<IBookingStore, EfBookingStore>();
        services.AddScoped<IReportStore, EfReportStore>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPropertyStore, EfPropertyStore>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<ILandlordContractStore, EfLandlordContractStore>();
        services.AddScoped<ILandlordContractService, LandlordContractService>();
        services.AddScoped<ITenantContractStore, EfTenantContractStore>();
        services.AddScoped<ITenantContractService, TenantContractService>();
        services.AddScoped<ILeadStore, EfLeadStore>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IDashboardStore, EfDashboardStore>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBookingService>(provider =>
        {
            var policy = new BookingPolicyOptions();
            if (int.TryParse(configuration["BookingPolicy:PublicCancellationHoursBeforeStart"], out var hours))
            {
                policy.PublicCancellationHoursBeforeStart = hours;
            }

            if (int.TryParse(configuration["BookingPolicy:LateCancellationFeePercent"], out var feePercent))
            {
                policy.LateCancellationFeePercent = feePercent;
            }

            if (int.TryParse(configuration["BookingPolicy:NoShowGraceMinutes"], out var noShowGraceMinutes))
            {
                policy.NoShowGraceMinutes = noShowGraceMinutes;
            }

            return new BookingService(
                provider.GetRequiredService<IBookingStore>(),
                provider.GetRequiredService<IBookingWriteLock>(),
                provider.GetRequiredService<ISystemClock>(),
                policy);
        });
        services.AddSingleton<IBookingWriteLock, BookingWriteLock>();
        services.AddScoped<DevelopmentFieldSeeder>();
        services.AddScoped<DevelopmentCommerceSeeder>();
        services.AddScoped<DevelopmentOperationsSeeder>();
        services.AddScoped<DevelopmentRealEstateSeeder>();

        return services;
    }

    private static string ResolveSqliteConnectionString(string connectionString, string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(contentRootPath))
        {
            return connectionString;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || Path.IsPathRooted(builder.DataSource)
            || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        return builder.ToString();
    }
}

