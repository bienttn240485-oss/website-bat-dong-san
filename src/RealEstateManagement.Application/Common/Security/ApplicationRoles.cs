namespace RealEstateManagement.Application.Common.Security;

public static class ApplicationRoles
{
    public const string Admin = nameof(Admin);
    public const string Sale = nameof(Sale);

    public const string Owner = Admin;
    public const string Staff = Sale;

    public static readonly string[] All = [Admin, Sale];
    public static readonly string[] Internal = [Admin, Sale];
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

