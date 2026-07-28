using FootballBooking.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FootballBooking.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
