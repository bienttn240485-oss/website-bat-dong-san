using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Tests.Domain;

public sealed class LeadTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WhenValid_CreatesLeadWithNewStatus()
    {
        var lead = CreateLead();

        Assert.Equal("Le Van C", lead.Name);
        Assert.Equal("0909000000", lead.Contact);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Equal("vi", lead.Language);
    }

    [Theory]
    [InlineData("", "0909000000")]
    [InlineData("Le Van C", "")]
    public void Constructor_WhenRequiredFieldsEmpty_Throws(string name, string contact)
    {
        Assert.Throws<ArgumentException>(() => CreateLead(name: name, contact: contact));
    }

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp()
    {
        var lead = CreateLead();
        var changedAt = Now.AddMinutes(30);

        lead.ChangeStatus(LeadStatus.Contacted, changedAt);

        Assert.Equal(LeadStatus.Contacted, lead.Status);
        Assert.Equal(changedAt, lead.UpdatedAtUtc);
    }

    [Fact]
    public void AssignTo_SetsSaleUserAndTimestamp()
    {
        var lead = CreateLead();
        var saleUserId = Guid.NewGuid();
        var assignedAt = Now.AddMinutes(45);

        lead.AssignTo(saleUserId, assignedAt);

        Assert.Equal(saleUserId, lead.AssignedToUserId);
        Assert.Equal(assignedAt, lead.UpdatedAtUtc);
    }

    private static Lead CreateLead(string name = "Le Van C", string contact = "0909000000")
        => new(
            Guid.NewGuid(),
            name,
            contact,
            Guid.NewGuid(),
            "Rent inquiry",
            "I want to view this apartment.",
            null,
            Now);
}
