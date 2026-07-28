namespace FootballBooking.Application.Common.Security;

public static class ApplicationRoles
{
    public const string Customer = nameof(Customer);
    public const string Owner = nameof(Owner);
    public const string Staff = nameof(Staff);

    public static readonly string[] All = [Customer, Owner, Staff];
    public static readonly string[] Internal = [Owner, Staff];
}
