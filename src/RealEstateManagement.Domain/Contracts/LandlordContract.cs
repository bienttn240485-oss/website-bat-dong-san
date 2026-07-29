namespace RealEstateManagement.Domain.Contracts;

public sealed class LandlordContract
{
    private LandlordContract()
    {
    }

    public LandlordContract(
        Guid id,
        Guid propertyId,
        string landlordName,
        string? peCode,
        string? saleName,
        long inputPrice,
        DateOnly signedDate,
        DateOnly? expiryDate,
        DepositStatus depositStatus,
        int? paymentDay,
        string? paymentWindow,
        DateOnly? nextDueDate,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Landlord contract ID is required.", nameof(id));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("Property ID is required.", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(landlordName))
        {
            throw new ArgumentException("Landlord name is required.", nameof(landlordName));
        }

        if (inputPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputPrice), "Input price cannot be negative.");
        }

        var normalizedExpiryDate = expiryDate ?? signedDate.AddMonths(12);
        if (normalizedExpiryDate <= signedDate)
        {
            throw new ArgumentException("Expiry date must be after signed date.", nameof(expiryDate));
        }

        if (paymentDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(paymentDay), "Payment day must be between 1 and 31.");
        }

        Id = id;
        PropertyId = propertyId;
        LandlordName = landlordName.Trim();
        PeCode = NormalizeOptional(peCode);
        SaleName = NormalizeOptional(saleName);
        InputPrice = inputPrice;
        SignedDate = signedDate;
        ExpiryDate = normalizedExpiryDate;
        DepositStatus = depositStatus;
        PaymentDay = paymentDay;
        PaymentWindow = NormalizeOptional(paymentWindow);
        NextDueDate = nextDueDate;
        Notes = NormalizeOptional(notes);
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string LandlordName { get; private set; } = string.Empty;
    public string? PeCode { get; private set; }
    public string? SaleName { get; private set; }
    public long InputPrice { get; private set; }
    public DateOnly SignedDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public DepositStatus DepositStatus { get; private set; }
    public int? PaymentDay { get; private set; }
    public string? PaymentWindow { get; private set; }
    public DateOnly? NextDueDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string landlordName,
        string? peCode,
        string? saleName,
        long inputPrice,
        DateOnly signedDate,
        DateOnly? expiryDate,
        DepositStatus depositStatus,
        int? paymentDay,
        string? paymentWindow,
        DateOnly? nextDueDate,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(landlordName))
        {
            throw new ArgumentException("Landlord name is required.", nameof(landlordName));
        }

        if (inputPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputPrice), "Input price cannot be negative.");
        }

        var normalizedExpiryDate = expiryDate ?? signedDate.AddMonths(12);
        if (normalizedExpiryDate <= signedDate)
        {
            throw new ArgumentException("Expiry date must be after signed date.", nameof(expiryDate));
        }

        if (paymentDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(paymentDay), "Payment day must be between 1 and 31.");
        }

        LandlordName = landlordName.Trim();
        PeCode = NormalizeOptional(peCode);
        SaleName = NormalizeOptional(saleName);
        InputPrice = inputPrice;
        SignedDate = signedDate;
        ExpiryDate = normalizedExpiryDate;
        DepositStatus = depositStatus;
        PaymentDay = paymentDay;
        PaymentWindow = NormalizeOptional(paymentWindow);
        NextDueDate = nextDueDate;
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
