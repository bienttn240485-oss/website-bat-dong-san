namespace RealEstateManagement.Infrastructure.Identity;

public sealed class DevelopmentInternalUsersOptions
{
    public List<DevelopmentInternalUserOptions> Users { get; set; } = [];
}

public sealed class DevelopmentInternalUserOptions
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? FullName { get; set; }

    public string? Role { get; set; }
}

