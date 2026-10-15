using Microsoft.AspNetCore.Mvc;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Controllers;

/// <summary>
/// Receives payment events from the card provider, so payments are settled (and
/// refunds made in the provider dashboard are recorded) without anyone calling
/// capture by hand.
/// </summary>
/// <remarks>
/// The provider retries any event that doesn't get a 2xx response, with backoff,
/// for up to 3 days.
/// </remarks>
[ApiController]
[Route("api/payments/webhook")]
public class PaymentWebhooksController(AppDbContext db, ILogger<PaymentWebhooksController> logger) : ControllerBase
{
    public const string SignatureHeader = "X-Payment-Signature";

    /// <summary>Handle a payment event from the provider.</summary>
    [HttpPost]
    public async Task<IActionResult> Receive(PaymentWebhookEvent evt)
    {
        // The provider signs every request; reject anything unsigned.
        if (string.IsNullOrEmpty(Request.Headers[SignatureHeader]))
            return Unauthorized();

        var payment = await db.Payments.FindAsync(evt.PaymentReference);
        if (payment is null)
            return NotFound();

        switch (evt.Type)
        {
            case PaymentEventType.PaymentSucceeded:
                payment.Status = PaymentStatus.Paid;
                break;

            case PaymentEventType.PaymentFailed:
                payment.Status = PaymentStatus.Failed;
                break;

            case PaymentEventType.RefundSucceeded:
                // Refunds are recorded as a separate negative payment against the booking.
                db.Payments.Add(new Payment
                {
                    BookingId = payment.BookingId,
                    Amount = -evt.Amount,
                    Status = PaymentStatus.Refunded,
                    Method = payment.Method,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                break;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Processed {EventType} event {EventId} for payment {PaymentId}",
            evt.Type, evt.EventId, payment.Id);

        return Ok();
    }
}
