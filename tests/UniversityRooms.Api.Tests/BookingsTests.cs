using System.Net;
using System.Net.Http.Json;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class BookingsTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    private static CreateBookingRequest NewRequest(int roomId, string email, DateOnly checkIn, int nights,
        PaymentMethod method = PaymentMethod.Card) => new()
    {
        RoomId = roomId,
        ContactEmail = email,
        CheckInDate = checkIn,
        CheckOutDate = checkIn.AddDays(nights),
        PaymentMethod = method,
    };

    [Fact]
    public async Task GetAll_returns_seeded_bookings()
    {
        var bookings = await (await _client.GetAsync("/api/bookings"))
            .ReadAsync<List<BookingResponse>>();

        Assert.True(bookings.Count >= 4);
        Assert.All(bookings, b => Assert.False(string.IsNullOrWhiteSpace(b.RoomName)));
    }

    [Fact]
    public async Task GetById_returns_booking()
    {
        var booking = await (await _client.GetAsync("/api/bookings/1"))
            .ReadAsync<BookingResponse>();

        Assert.Equal(1, booking.Id);
        Assert.Equal("Confirmed", booking.Status);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        var response = await _client.GetAsync("/api/bookings/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_books_a_room_prices_it_and_records_a_payment()
    {
        var checkIn = new DateOnly(2027, 1, 10);
        var request = NewRequest(3, "create-test@example.com", checkIn, nights: 2, PaymentMethod.Invoice);

        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var booking = await response.ReadAsync<BookingResponse>();
        Assert.Equal("Confirmed", booking.Status);
        Assert.Equal(3, booking.RoomId);
        Assert.Equal(2, booking.Nights);
        Assert.Equal("create-test@example.com", booking.ContactEmail);
        Assert.True(booking.TotalPrice > 0);

        var payments = await (await _client.GetAsync($"/api/payments?bookingId={booking.Id}"))
            .ReadAsync<List<PaymentResponse>>();
        var payment = Assert.Single(payments);
        Assert.Equal(booking.TotalPrice, payment.Amount);
        Assert.Equal("Pending", payment.Status);
        Assert.Equal("Invoice", payment.Method);
    }

    [Fact]
    public async Task GetAll_can_filter_by_email()
    {
        var checkIn = new DateOnly(2027, 1, 11);
        await _client.PostAsJsonAsync("/api/bookings", NewRequest(4, "filter-test@example.com", checkIn, nights: 1));

        var bookings = await (await _client.GetAsync("/api/bookings?email=filter-test@example.com"))
            .ReadAsync<List<BookingResponse>>();

        var booking = Assert.Single(bookings);
        Assert.Equal("filter-test@example.com", booking.ContactEmail);
    }

    [Fact]
    public async Task Create_with_checkout_not_after_checkin_returns_400()
    {
        var checkIn = new DateOnly(2027, 1, 12);
        var request = NewRequest(3, "bad-range@example.com", checkIn, nights: 0);

        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_for_unknown_room_returns_404()
    {
        var checkIn = new DateOnly(2027, 1, 13);
        var request = NewRequest(99999, "no-room@example.com", checkIn, nights: 1);

        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_overlapping_booking_returns_409()
    {
        var checkIn = new DateOnly(2027, 1, 14);
        var first = await _client.PostAsJsonAsync("/api/bookings", NewRequest(5, "first@example.com", checkIn, nights: 3));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var clashing = NewRequest(5, "second@example.com", checkIn.AddDays(1), nights: 3);
        var response = await _client.PostAsJsonAsync("/api/bookings", clashing);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_marks_booking_cancelled()
    {
        var checkIn = new DateOnly(2027, 1, 20);
        var created = await (await _client.PostAsJsonAsync("/api/bookings", NewRequest(6, "cancel-test@example.com", checkIn, nights: 1)))
            .ReadAsync<BookingResponse>();

        var cancel = await _client.PostAsync($"/api/bookings/{created.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var booking = await (await _client.GetAsync($"/api/bookings/{created.Id}"))
            .ReadAsync<BookingResponse>();
        Assert.Equal("Cancelled", booking.Status);
    }

    [Fact]
    public async Task Cancel_unknown_booking_returns_404()
    {
        var response = await _client.PostAsync("/api/bookings/99999/cancel", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_dates()
    {
        var created = await (await _client.PostAsJsonAsync("/api/bookings",
            NewRequest(7, "modify-test@example.com", new DateOnly(2027, 8, 1), nights: 1))).ReadAsync<BookingResponse>();

        var update = await _client.PutAsJsonAsync($"/api/bookings/{created.Id}", new UpdateBookingRequest
        {
            CheckInDate = new DateOnly(2027, 9, 10),
            CheckOutDate = new DateOnly(2027, 9, 12),
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var booking = await update.ReadAsync<BookingResponse>();
        Assert.Equal(new DateOnly(2027, 9, 10), booking.CheckInDate);
        Assert.Equal(new DateOnly(2027, 9, 12), booking.CheckOutDate);
        Assert.Equal(2, booking.Nights);
    }

    [Fact]
    public async Task Update_unknown_booking_returns_404()
    {
        var response = await _client.PutAsJsonAsync("/api/bookings/99999", new UpdateBookingRequest
        {
            CheckInDate = new DateOnly(2027, 9, 1),
            CheckOutDate = new DateOnly(2027, 9, 2),
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
