namespace RealEstateManagement.Domain.Bookings;

public sealed class PaymentRecord
{
    private PaymentRecord()
    {
    }

    public PaymentRecord(
        Guid id,
        Guid bookingId,
        PaymentRecordType paymentType,
        PaymentMethod method,
        long amount,
        PaymentRecordStatus status,
        string? transactionCode,
        string? note,
        Guid? recordedByUserId,
        DateTimeOffset utcNow)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Số tiá»n giao dịch phải lớn hơn 0.");
        }

        Id = id;
        BookingId = bookingId;
        PaymentType = paymentType;
        Method = method;
        Amount = amount;
        Status = status;
        TransactionCode = string.IsNullOrWhiteSpace(transactionCode) ? null : transactionCode.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        RecordedByUserId = recordedByUserId;
        ProcessedAtUtc = status == PaymentRecordStatus.Succeeded ? utcNow : null;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!;
    public PaymentRecordType PaymentType { get; private set; }
    public PaymentMethod Method { get; private set; }
    public long Amount { get; private set; }
    public PaymentRecordStatus Status { get; private set; }
    public string? TransactionCode { get; private set; }
    public string? Note { get; private set; }
    public string? EvidencePath { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

