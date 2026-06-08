using System.Net;
using System.Net.Http.Json;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class PaymentsTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task GetAll_returns_seeded_payments()
    {
        var payments = await (await _client.GetAsync("/api/payments"))
            .ReadAsync<List<PaymentResponse>>();

        Assert.True(payments.Count >= 4);
    }

    [Fact]
    public async Task GetById_returns_payment()
    {
        var payment = await (await _client.GetAsync("/api/payments/1"))
            .ReadAsync<PaymentResponse>();

        Assert.Equal(1, payment.Id);
        Assert.True(payment.Amount > 0);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        var response = await _client.GetAsync("/api/payments/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Capture_marks_a_pending_payment_paid()
    {
        // Make a booking so we have a fresh pending payment to capture.
        var checkIn = new DateOnly(2027, 2, 1);
        var booking = await (await _client.PostAsJsonAsync("/api/bookings", new CreateBookingRequest
        {
            RoomId = 7,
            ContactEmail = "capture-test@example.com",
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            PaymentMethod = PaymentMethod.Card,
        })).ReadAsync<BookingResponse>();

        var payments = await (await _client.GetAsync($"/api/payments?bookingId={booking.Id}"))
            .ReadAsync<List<PaymentResponse>>();
        var pending = Assert.Single(payments);
        Assert.Equal("Pending", pending.Status);

        var captured = await (await _client.PostAsync($"/api/payments/{pending.Id}/capture", null))
            .ReadAsync<PaymentResponse>();
        Assert.Equal("Paid", captured.Status);
    }

    [Fact]
    public async Task Capture_unknown_payment_returns_404()
    {
        var response = await _client.PostAsync("/api/payments/99999/capture", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
