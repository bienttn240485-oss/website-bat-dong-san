using RealEstateManagement.Application.Common.Security;

namespace RealEstateManagement.Tests;

public sealed class ApplicationRolesTests
{
    [Fact]
    public void All_WhenRead_ContainsTechnicalRoles()
    {
        Assert.Contains(ApplicationRoles.Customer, ApplicationRoles.All);
        Assert.Contains(ApplicationRoles.Owner, ApplicationRoles.All);
        Assert.Contains(ApplicationRoles.Staff, ApplicationRoles.All);
    }
}

