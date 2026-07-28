using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Fields;
using RealEstateManagement.Domain.Fields;

namespace RealEstateManagement.Tests.Application;

public sealed class FieldServiceTests
{
    [Fact]
    public async Task CreateFieldAsync_WhenOperatingHourInvalid_ReturnsVietnameseValidationError()
    {
        var store = new InMemoryFieldStore();
        var service = new FieldService(store, new FixedClock());
        var command = ValidCommand() with
        {
            OperatingHours = Enumerable.Range(0, 7)
                .Select(day => new FieldOperatingHourCommand(day, false, 900, 360))
                .ToArray()
        };

        var result = await service.CreateFieldAsync(command);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Giá» hoạt động", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateFieldAsync_WhenDuplicateSlug_ReturnsFailure()
    {
        var store = new InMemoryFieldStore { ExistingSlug = "san-5a" };
        var service = new FieldService(store, new FixedClock());

        var result = await service.CreateFieldAsync(ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Contains("ÄÆ°á»ng dẫn công khai đã tồn tại.", result.Errors);
    }

    private static FieldEditorCommand ValidCommand()
        => new(
            "F5A",
            "Sân 5A",
            "san-5a",
            "Sân 5 ngÆ°á»i",
            10,
            "Sân cá» nhân tạo.",
            "12 Ä‘Æ°á»ng D5, PhÆ°á»ng 25, Quận Bình Thạnh",
            ["Cá» nhân tạo", "ÄÃ¨n LED"],
            60,
            30,
            FieldStatus.Active,
            [new FieldImageCommand("/images/fields/san-5a.svg", "Ảnh Sân 5A", 1, true)],
            Enumerable.Range(0, 7).Select(day => new FieldOperatingHourCommand(day, false, 360, 1380)).ToArray(),
            [new PricingRuleCommand("Giá buổi tối", PricingRuleType.Weekday, null, null, new DateOnly(2026, 1, 1), null, 1080, 1380, 250000, 10, true)]);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class InMemoryFieldStore : IFieldStore
    {
        public string? ExistingSlug { get; init; }

        public Task<IReadOnlyList<FieldSummaryDto>> ListPublicFieldsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FieldSummaryDto>>([]);

        public Task<IReadOnlyList<FieldSummaryDto>> ListAdminFieldsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FieldSummaryDto>>([]);

        public Task<FieldDetailDto?> GetFieldDetailBySlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<FieldDetailDto?>(null);

        public Task<FieldDetailDto?> GetFieldDetailByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<FieldDetailDto?>(null);

        public Task<Field?> GetFieldForUpdateAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Field?>(null);

        public Task<bool> CodeExistsAsync(string code, Guid? exceptFieldId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> SlugExistsAsync(string slug, Guid? exceptFieldId, CancellationToken cancellationToken) => Task.FromResult(slug == ExistingSlug);

        public Task AddFieldAsync(Field field, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

