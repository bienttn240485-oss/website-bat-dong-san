using FootballBooking.Application.Common.Time;
using FootballBooking.Application.Bookings;
using FootballBooking.Application.Fields;
using FootballBooking.Infrastructure.Bookings;
using FootballBooking.Infrastructure.Data;
using FootballBooking.Infrastructure.Fields;
using FootballBooking.Infrastructure.Identity;
using FootballBooking.Infrastructure.Reports;
using FootballBooking.Infrastructure.SeedData;
using FootballBooking.Infrastructure.Time;
using FootballBooking.Application.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FootballBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string? contentRootPath = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=App_Data/football-booking.db";
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
