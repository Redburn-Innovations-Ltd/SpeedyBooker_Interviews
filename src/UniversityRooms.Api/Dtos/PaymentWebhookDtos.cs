using System.ComponentModel.DataAnnotations;

namespace UniversityRooms.Api.Dtos;

public enum PaymentEventType
{
    PaymentSucceeded,
    PaymentFailed,
    RefundSucceeded,
}

/// <summary>
/// An event the card provider posts to us when something happens to a payment.
/// </summary>
public class PaymentWebhookEvent
{
    /// <summary>The provider's unique id for this event, e.g. "evt_1Q2w3E4r".</summary>
    [Required]
    public required string EventId { get; set; }

    [Required]
    public PaymentEventType Type { get; set; }

    /// <summary>Our payment id, which we send to the provider as the reference when checkout starts.</summary>
    [Required]
    public int PaymentReference { get; set; }

    /// <summary>Amount in GBP that the event is for.</summary>
    public decimal Amount { get; set; }

    /// <summary>When the event happened at the provider.</summary>
    public DateTime OccurredAtUtc { get; set; }
}
