namespace RealEstateManagement.Application.Common.Security;

public static class ApplicationRoles
{
    public const string Customer = nameof(Customer);
    public const string Owner = nameof(Owner);
    public const string Staff = nameof(Staff);

    public static readonly string[] All = [Customer, Owner, Staff];
    public static readonly string[] Internal = [Owner, Staff];
}

public static class AuthorizationPolicies
{
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireAdminOrSale = nameof(RequireAdminOrSale);
    public const string CanManageProperties = nameof(CanManageProperties);
    public const string CanManageContracts = nameof(CanManageContracts);
    public const string CanViewFinancialDashboard = nameof(CanViewFinancialDashboard);
    public const string CanManageStaff = nameof(CanManageStaff);
    public const string CanAssignLeads = nameof(CanAssignLeads);
    public const string CanUpdateAssignedLead = nameof(CanUpdateAssignedLead);
}

