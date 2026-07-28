namespace RealEstateManagement.Domain.Bookings;

public sealed class PromoCode
{
    private PromoCode()
    {
    }

    public PromoCode(
        Guid id,
        string code,
        string name,
        PromoDiscountType discountType,
        long discountValue,
        long? maximumDiscountAmount,
        long minimumOrderAmount,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        int? totalUsageLimit,
        int? perPhoneUsageLimit,
        Guid? applicableFieldId,
        int? applicableStartMinute,
        int? applicableEndMinute,
        bool isActive,
        DateTimeOffset utcNow)
    {
        Id = id;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        DiscountType = discountType;
        DiscountValue = discountValue;
        MaximumDiscountAmount = maximumDiscountAmount;
        MinimumOrderAmount = minimumOrderAmount;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        TotalUsageLimit = totalUsageLimit;
        PerPhoneUsageLimit = perPhoneUsageLimit;
        ApplicableFieldId = applicableFieldId;
        ApplicableStartMinute = applicableStartMinute;
        ApplicableEndMinute = applicableEndMinute;
        IsActive = isActive;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PromoDiscountType DiscountType { get; private set; }
    public long DiscountValue { get; private set; }
    public long? MaximumDiscountAmount { get; private set; }
    public long MinimumOrderAmount { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public int? TotalUsageLimit { get; private set; }
    public int? PerPhoneUsageLimit { get; private set; }
    public Guid? ApplicableFieldId { get; private set; }
    public int? ApplicableStartMinute { get; private set; }
    public int? ApplicableEndMinute { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long CalculateDiscount(long eligibleAmount)
    {
        if (eligibleAmount <= 0)
        {
            return 0;
        }

        var rawDiscount = DiscountType == PromoDiscountType.FixedAmount
            ? DiscountValue
            : eligibleAmount * DiscountValue / 10000;

        if (MaximumDiscountAmount is not null)
        {
            rawDiscount = Math.Min(rawDiscount, MaximumDiscountAmount.Value);
        }

        return Math.Min(rawDiscount, eligibleAmount);
    }

    public void Update(
        string code,
        string name,
        PromoDiscountType discountType,
        long discountValue,
        long? maximumDiscountAmount,
        long minimumOrderAmount,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        int? totalUsageLimit,
        int? perPhoneUsageLimit,
        Guid? applicableFieldId,
        int? applicableStartMinute,
        int? applicableEndMinute,
        bool isActive,
        DateTimeOffset utcNow)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        DiscountType = discountType;
        DiscountValue = discountValue;
        MaximumDiscountAmount = maximumDiscountAmount;
        MinimumOrderAmount = minimumOrderAmount;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        TotalUsageLimit = totalUsageLimit;
        PerPhoneUsageLimit = perPhoneUsageLimit;
        ApplicableFieldId = applicableFieldId;
        ApplicableStartMinute = applicableStartMinute;
        ApplicableEndMinute = applicableEndMinute;
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }

    public void SetActive(bool isActive, DateTimeOffset utcNow)
    {
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }
}

