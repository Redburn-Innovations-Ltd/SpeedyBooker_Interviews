using System.Net;
using System.Net.Http.Json;
using UniversityRooms.Api.Controllers;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class PaymentWebhookTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    private async Task<PaymentResponse> PendingPaymentAsync(int roomId, DateOnly checkIn)
    {
        var booking = await (await _client.PostAsJsonAsync("/api/bookings", new CreateBookingRequest
        {
            RoomId = roomId,
            ContactEmail = "webhook-test@example.com",
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            PaymentMethod = PaymentMethod.Card,
        })).ReadAsync<BookingResponse>();

        var payments = await (await _client.GetAsync($"/api/payments?bookingId={booking.Id}"))
            .ReadAsync<List<PaymentResponse>>();
        return Assert.Single(payments);
    }

    private async Task<HttpResponseMessage> SendAsync(PaymentEventType type, int paymentId, decimal amount, bool signed = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook")
        {
            Content = JsonContent.Create(new PaymentWebhookEvent
            {
                EventId = $"evt_{Guid.NewGuid():N}",
                Type = type,
                PaymentReference = paymentId,
                Amount = amount,
                OccurredAtUtc = DateTime.UtcNow,
            }),
        };

        if (signed)
            request.Headers.Add(PaymentWebhooksController.SignatureHeader, "t=1760000000,v1=test-signature");

        return await _client.SendAsync(request);
    }

    private async Task<PaymentResponse> PaymentAsync(int id) =>
        await (await _client.GetAsync($"/api/payments/{id}")).ReadAsync<PaymentResponse>();

    [Fact]
    public async Task Succeeded_event_marks_the_payment_paid()
    {
        var payment = await PendingPaymentAsync(1, new DateOnly(2028, 3, 1));

        var response = await SendAsync(PaymentEventType.PaymentSucceeded, payment.Id, payment.Amount);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Paid", (await PaymentAsync(payment.Id)).Status);
    }

    [Fact]
    public async Task Failed_event_marks_the_payment_failed()
    {
        var payment = await PendingPaymentAsync(2, new DateOnly(2028, 3, 1));

        await SendAsync(PaymentEventType.PaymentFailed, payment.Id, payment.Amount);

        Assert.Equal("Failed", (await PaymentAsync(payment.Id)).Status);
    }

    [Fact]
    public async Task Refund_event_records_a_negative_payment()
    {
        var payment = await PendingPaymentAsync(3, new DateOnly(2028, 3, 1));
        await SendAsync(PaymentEventType.PaymentSucceeded, payment.Id, payment.Amount);

        await SendAsync(PaymentEventType.RefundSucceeded, payment.Id, payment.Amount);

        var payments = await (await _client.GetAsync($"/api/payments?bookingId={payment.BookingId}"))
            .ReadAsync<List<PaymentResponse>>();
        var refund = Assert.Single(payments, p => p.Status == "Refunded");
        Assert.Equal(-payment.Amount, refund.Amount);
    }

    [Fact]
    public async Task Unsigned_event_is_rejected()
    {
        var payment = await PendingPaymentAsync(4, new DateOnly(2028, 3, 1));

        var response = await SendAsync(PaymentEventType.PaymentSucceeded, payment.Id, payment.Amount, signed: false);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Pending", (await PaymentAsync(payment.Id)).Status);
    }

    [Fact]
    public async Task Event_for_unknown_payment_returns_404()
    {
        var response = await SendAsync(PaymentEventType.PaymentSucceeded, 99999, 10m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
