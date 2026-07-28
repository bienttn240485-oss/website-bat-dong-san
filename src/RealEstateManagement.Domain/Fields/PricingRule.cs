namespace RealEstateManagement.Domain.Fields;

public sealed class PricingRule
{
    private PricingRule()
    {
    }

    public PricingRule(
        Guid id,
        Guid fieldId,
        string name,
        PricingRuleType ruleType,
        DateOnly? specificDate,
        int? dayOfWeek,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        int startMinute,
        int endMinute,
        long pricePerHour,
        int priority,
        bool isActive,
        DateTimeOffset utcNow)
    {
        Id = id;
        FieldId = fieldId;
        Update(name, ruleType, specificDate, dayOfWeek, effectiveFrom, effectiveTo, startMinute, endMinute, pricePerHour, priority, isActive, utcNow);
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid FieldId { get; private set; }
    public Field Field { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public PricingRuleType RuleType { get; private set; }
    public DateOnly? SpecificDate { get; private set; }
    public int? DayOfWeek { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public int StartMinute { get; private set; }
    public int EndMinute { get; private set; }
    public long PricePerHour { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string name,
        PricingRuleType ruleType,
        DateOnly? specificDate,
        int? dayOfWeek,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        int startMinute,
        int endMinute,
        long pricePerHour,
        int priority,
        bool isActive,
        DateTimeOffset utcNow)
    {
        Name = name.Trim();
        RuleType = ruleType;
        SpecificDate = specificDate;
        DayOfWeek = dayOfWeek;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        StartMinute = startMinute;
        EndMinute = endMinute;
        PricePerHour = pricePerHour;
        Priority = priority;
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }
}

