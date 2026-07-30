using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Application.Leads;

public sealed class LeadService(ILeadStore store, ISystemClock clock) : ILeadService
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase) { "vi", "en" };

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
            NormalizeLanguage(command.Language),
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

        lead.UpdateDetails(command.Name, command.Contact, command.PropertyId, command.Subject, command.Message, NormalizeLanguage(command.Language), clock.UtcNow);
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

        if (!CanActorManageLead(command.ActorUserId, command.ActorCanManageAll, lead))
        {
            return LeadCommandResult.Failure(["Bạn không có quyền cập nhật lead này."]);
        }

        if (!CanChangeStatus(lead.Status, command.Status))
        {
            return LeadCommandResult.Failure(["Trạng thái lead không hợp lệ."]);
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

        if (!command.ActorCanManageAll)
        {
            if (command.ActorUserId is null || command.ActorUserId != command.SaleUserId)
            {
                return LeadCommandResult.Failure(["Bạn không có quyền phân công lead này."]);
            }

            if (lead.AssignedToUserId is not null && lead.AssignedToUserId != command.ActorUserId)
            {
                return LeadCommandResult.Failure(["Bạn không có quyền phân công lead này."]);
            }
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
        MaxLength(command.Name, 160, "Tên khách hàng không được vượt quá 160 ký tự.", errors);
        MaxLength(command.Contact, 160, "Thông tin liên hệ không được vượt quá 160 ký tự.", errors);
        MaxLength(command.Subject, 200, "Chủ đề không được vượt quá 200 ký tự.", errors);
        MaxLength(command.Message, 4000, "Nội dung tư vấn không được vượt quá 4000 ký tự.", errors);

        if (!SupportedLanguages.Contains(NormalizeLanguage(command.Language)))
        {
            errors.Add("Ngôn ngữ không hợp lệ.");
        }

        if (command.PropertyId is { } propertyId && !await store.PropertyExistsAsync(propertyId, cancellationToken))
        {
            errors.Add("Không tìm thấy căn hộ liên quan đến lead.");
        }

        return errors;
    }

    private static LeadFilterQuery NormalizeQuery(LeadFilterQuery query)
        => query with
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
            Area = string.IsNullOrWhiteSpace(query.Area) ? null : query.Area.Trim(),
            Language = string.IsNullOrWhiteSpace(query.Language) ? null : NormalizeLanguage(query.Language)
        };

    private static void Required(string? value, string message, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }

    private static void MaxLength(string? value, int maxLength, string message, List<string> errors)
    {
        if (value?.Length > maxLength)
        {
            errors.Add(message);
        }
    }

    private static string NormalizeLanguage(string? language)
        => string.IsNullOrWhiteSpace(language) ? "vi" : language.Trim().ToLowerInvariant();

    private static bool CanActorManageLead(Guid? actorUserId, bool actorCanManageAll, Lead lead)
        => actorCanManageAll || actorUserId is not null && lead.AssignedToUserId == actorUserId;

    private static bool CanChangeStatus(LeadStatus current, LeadStatus next)
        => current == next
            || current == LeadStatus.New && next is LeadStatus.Contacted or LeadStatus.Lost
            || current == LeadStatus.Contacted && next is LeadStatus.Viewing or LeadStatus.Lost
            || current == LeadStatus.Viewing && next is LeadStatus.Converted or LeadStatus.Lost
            || current == LeadStatus.Converted && next == LeadStatus.Lost;
}
