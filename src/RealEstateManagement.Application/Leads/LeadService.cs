using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Application.Leads;

public sealed class LeadService(ILeadStore store, ISystemClock clock) : ILeadService
{
    public Task<IReadOnlyList<LeadDto>> ListLeadsAsync(LeadFilterQuery query, CancellationToken cancellationToken = default)
        => store.ListLeadsAsync(NormalizeQuery(query), cancellationToken);

    public Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetLeadAsync(id, cancellationToken);

    public async Task<LeadCommandResult> CreateLeadAsync(LeadCreateCommand command, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateCreateAsync(command, cancellationToken);
        if (errors.Count > 0)
        {
            return LeadCommandResult.Failure(errors);
        }

        var lead = new Lead(
            Guid.NewGuid(),
            command.Name,
            command.Contact,
            command.PropertyId,
            command.Subject,
            command.Message,
            command.Language,
            clock.UtcNow);

        await store.AddLeadAsync(lead, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return LeadCommandResult.Success(lead.Id);
    }

    public async Task<LeadCommandResult> UpdateLeadAsync(Guid leadId, LeadUpdateCommand command, CancellationToken cancellationToken = default)
    {
        var lead = await store.GetLeadForUpdateAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return LeadCommandResult.Failure(["Không tìm thấy lead cần cập nhật."]);
        }

        var createLikeCommand = new LeadCreateCommand(command.Name, command.Contact, command.PropertyId, command.Subject, command.Message, command.Language);
        var errors = await ValidateCreateAsync(createLikeCommand, cancellationToken);
        if (errors.Count > 0)
        {
            return LeadCommandResult.Failure(errors);
        }

        lead.UpdateDetails(command.Name, command.Contact, command.PropertyId, command.Subject, command.Message, command.Language, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        return LeadCommandResult.Success(lead.Id);
    }

    public async Task<LeadCommandResult> ChangeStatusAsync(LeadStatusCommand command, CancellationToken cancellationToken = default)
    {
        var lead = await store.GetLeadForUpdateAsync(command.LeadId, cancellationToken);
        if (lead is null)
        {
            return LeadCommandResult.Failure(["Không tìm thấy lead cần cập nhật."]);
        }

        lead.ChangeStatus(command.Status, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        return LeadCommandResult.Success(lead.Id);
    }

    public async Task<LeadCommandResult> AssignLeadAsync(LeadAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        var lead = await store.GetLeadForUpdateAsync(command.LeadId, cancellationToken);
        if (lead is null)
        {
            return LeadCommandResult.Failure(["Không tìm thấy lead cần phân công."]);
        }

        if (command.SaleUserId == Guid.Empty)
        {
            return LeadCommandResult.Failure(["Vui lòng chọn nhân viên Sale."]);
        }

        if (!await store.SaleUserExistsAsync(command.SaleUserId, cancellationToken))
        {
            return LeadCommandResult.Failure(["Không tìm thấy nhân viên Sale được phân công."]);
        }

        lead.AssignTo(command.SaleUserId, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        return LeadCommandResult.Success(lead.Id);
    }

    private async Task<List<string>> ValidateCreateAsync(LeadCreateCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        Required(command.Name, "Vui lòng nhập tên khách hàng.", errors);
        Required(command.Contact, "Vui lòng nhập thông tin liên hệ.", errors);

        if (command.PropertyId is { } propertyId && !await store.PropertyExistsAsync(propertyId, cancellationToken))
        {
            errors.Add("Không tìm thấy căn hộ liên quan đến lead.");
        }

        return errors;
    }

    private static LeadFilterQuery NormalizeQuery(LeadFilterQuery query)
        => query with
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim()
        };

    private static void Required(string? value, string message, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }
}
