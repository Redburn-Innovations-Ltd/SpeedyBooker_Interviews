namespace UniversityRooms.Api.Models;

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Refunded = 2,
    Failed = 3
}

public enum PaymentMethod
{
    Card = 0,
    BankTransfer = 1,
    Invoice = 2
}

/// <summary>
/// A payment recorded against a <see cref="Booking"/>. A booking may have more
/// than one (e.g. a deposit followed by a balance, or a later refund).
/// Funds are settled with the vendor that owns the booked room.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>Amount in GBP. Refunds are stored as negative amounts.</summary>
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
