using RealEstateManagement.Application.Common.Security;

namespace RealEstateManagement.Tests;

public sealed class ApplicationRolesTests
{
    [Fact]
    public void All_WhenRead_ContainsTechnicalRoles()
    {
        Assert.Equal([ApplicationRoles.Admin, ApplicationRoles.Sale], ApplicationRoles.All);
        Assert.Equal(ApplicationRoles.Admin, ApplicationRoles.Owner);
        Assert.Equal(ApplicationRoles.Sale, ApplicationRoles.Staff);
    }
}

