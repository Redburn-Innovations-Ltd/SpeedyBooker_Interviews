using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;
using UniversityRooms.Api.Services;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class BookingHoldTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    private async Task<BookingResponse> BookAsync(int roomId, string email, DateOnly checkIn, int nights = 2)
    {
        var response = await _client.PostAsJsonAsync("/api/bookings", new CreateBookingRequest
        {
            RoomId = roomId,
            ContactEmail = email,
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(nights),
            PaymentMethod = PaymentMethod.Card,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<BookingResponse>();
    }

    private async Task<int> ReleaseHoldsCreatedBeforeAsync(DateTime cutoffUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var holds = scope.ServiceProvider.GetRequiredService<BookingHoldService>();
        return await holds.ReleaseExpiredHoldsAsync(cutoffUtc);
    }

    private async Task<string> StatusOfAsync(int bookingId) =>
        (await (await _client.GetAsync($"/api/bookings/{bookingId}")).ReadAsync<BookingResponse>()).Status;

    [Fact]
    public async Task New_booking_is_held_until_payment_is_captured()
    {
        var booking = await BookAsync(1, "hold-capture@example.com", new DateOnly(2029, 9, 3));
        Assert.Equal("Pending", booking.Status);

        var payment = Assert.Single(await (await _client.GetAsync($"/api/payments?bookingId={booking.Id}"))
            .ReadAsync<List<PaymentResponse>>());
        await _client.PostAsync($"/api/payments/{payment.Id}/capture", null);

        Assert.Equal("Confirmed", await StatusOfAsync(booking.Id));
    }

    [Fact]
    public async Task Expired_holds_are_released()
    {
        var booking = await BookAsync(2, "hold-expired@example.com", new DateOnly(2029, 9, 10));

        var released = await ReleaseHoldsCreatedBeforeAsync(DateTime.UtcNow.AddMinutes(1));

        Assert.True(released >= 1);
        Assert.Equal("Cancelled", await StatusOfAsync(booking.Id));
    }

    [Fact]
    public async Task Recent_holds_are_kept()
    {
        var booking = await BookAsync(2, "hold-recent@example.com", new DateOnly(2029, 9, 20));

        await ReleaseHoldsCreatedBeforeAsync(DateTime.UtcNow - BookingHoldService.HoldDuration);

        Assert.Equal("Pending", await StatusOfAsync(booking.Id));
    }

    [Fact]
    public async Task Released_room_can_be_booked_again()
    {
        var checkIn = new DateOnly(2029, 10, 1);
        await BookAsync(1, "hold-abandoned@example.com", checkIn);

        await ReleaseHoldsCreatedBeforeAsync(DateTime.UtcNow.AddMinutes(1));

        var second = await BookAsync(1, "hold-next-guest@example.com", checkIn);
        Assert.Equal("Pending", second.Status);
    }
}
