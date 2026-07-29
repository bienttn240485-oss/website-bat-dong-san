namespace RealEstateManagement.Domain.Contracts;

public sealed class TenantContract
{
    private TenantContract()
    {
    }

    public TenantContract(
        Guid id,
        Guid propertyId,
        string tenantName,
        string? managerName,
        long rentalPrice,
        DateOnly signedDate,
        int termMonths,
        long depositAmount,
        DateOnly? depositReturnDate,
        string? peCode,
        string? passCode,
        ContractStatus status,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Tenant contract ID is required.", nameof(id));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("Property ID is required.", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name is required.", nameof(tenantName));
        }

        if (rentalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rentalPrice), "Rental price cannot be negative.");
        }

        if (depositAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depositAmount), "Deposit amount cannot be negative.");
        }

        if (termMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(termMonths), "Term months must be greater than zero.");
        }

        Id = id;
        PropertyId = propertyId;
        TenantName = tenantName.Trim();
        ManagerName = NormalizeOptional(managerName);
        RentalPrice = rentalPrice;
        SignedDate = signedDate;
        TermMonths = termMonths;
        ExpiryDate = signedDate.AddMonths(termMonths);
        DepositAmount = depositAmount;
        DepositReturnDate = depositReturnDate;
        PeCode = NormalizeOptional(peCode);
        PassCode = NormalizeOptional(passCode);
        Status = status;
        Notes = NormalizeOptional(notes);
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string TenantName { get; private set; } = string.Empty;
    public string? ManagerName { get; private set; }
    public long RentalPrice { get; private set; }
    public DateOnly SignedDate { get; private set; }
    public int TermMonths { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public long DepositAmount { get; private set; }
    public DateOnly? DepositReturnDate { get; private set; }
    public string? PeCode { get; private set; }
    public string? PassCode { get; private set; }
    public ContractStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string tenantName,
        string? managerName,
        long rentalPrice,
        DateOnly signedDate,
        int termMonths,
        long depositAmount,
        DateOnly? depositReturnDate,
        string? peCode,
        string? passCode,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name is required.", nameof(tenantName));
        }

        if (rentalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rentalPrice), "Rental price cannot be negative.");
        }

        if (depositAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depositAmount), "Deposit amount cannot be negative.");
        }

        if (termMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(termMonths), "Term months must be greater than zero.");
        }

        TenantName = tenantName.Trim();
        ManagerName = NormalizeOptional(managerName);
        RentalPrice = rentalPrice;
        SignedDate = signedDate;
        TermMonths = termMonths;
        ExpiryDate = signedDate.AddMonths(termMonths);
        DepositAmount = depositAmount;
        DepositReturnDate = depositReturnDate;
        PeCode = NormalizeOptional(peCode);
        PassCode = NormalizeOptional(passCode);
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = utcNow;
    }

    public void ChangeStatus(ContractStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        UpdatedAtUtc = utcNow;
    }

    public bool Overlaps(DateOnly signedDate, DateOnly expiryDate)
        => signedDate < ExpiryDate && expiryDate > SignedDate;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
