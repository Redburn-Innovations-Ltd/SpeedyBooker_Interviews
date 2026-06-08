using System.Net;
using System.Net.Http.Json;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class RoomsTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task GetAll_returns_all_rooms()
    {
        var rooms = await (await _client.GetAsync("/api/rooms"))
            .ReadAsync<List<RoomResponse>>();

        Assert.True(rooms.Count >= 7);
        Assert.All(rooms, r => Assert.False(string.IsNullOrWhiteSpace(r.VendorName)));
    }

    [Fact]
    public async Task GetAll_filters_by_minimum_capacity()
    {
        var rooms = await (await _client.GetAsync("/api/rooms?minCapacity=2"))
            .ReadAsync<List<RoomResponse>>();

        Assert.NotEmpty(rooms);
        Assert.All(rooms, r => Assert.True(r.Capacity >= 2));
    }

    [Fact]
    public async Task GetAll_filters_by_vendor()
    {
        var rooms = await (await _client.GetAsync("/api/rooms?vendorId=2"))
            .ReadAsync<List<RoomResponse>>();

        Assert.NotEmpty(rooms);
        Assert.All(rooms, r => Assert.Equal(2, r.VendorId));
    }

    [Fact]
    public async Task GetById_returns_room()
    {
        var room = await (await _client.GetAsync("/api/rooms/1"))
            .ReadAsync<RoomResponse>();

        Assert.Equal(1, room.Id);
        Assert.True(room.NightlyRate > 0);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        var response = await _client.GetAsync("/api/rooms/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBookings_returns_bookings_for_room()
    {
        var bookings = await (await _client.GetAsync("/api/rooms/1/bookings"))
            .ReadAsync<List<BookingResponse>>();

        Assert.All(bookings, b => Assert.Equal(1, b.RoomId));
    }

    [Fact]
    public async Task GetBookings_for_unknown_room_returns_404()
    {
        var response = await _client.GetAsync("/api/rooms/99999/bookings");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Availability_returns_free_rooms()
    {
        var rooms = await (await _client.GetAsync("/api/rooms/availability?startDate=2030-01-01&nights=2"))
            .ReadAsync<List<RoomResponse>>();

        Assert.NotEmpty(rooms);
        Assert.All(rooms, r => Assert.False(string.IsNullOrWhiteSpace(r.VendorName)));
    }

    [Fact]
    public async Task Availability_excludes_a_room_booked_for_the_window()
    {
        // Book room 2 for two nights, then ask what's free over the same window.
        await _client.PostAsJsonAsync("/api/bookings", new CreateBookingRequest
        {
            RoomId = 2,
            ContactEmail = "availability-spec@example.com",
            CheckInDate = new DateOnly(2030, 3, 1),
            CheckOutDate = new DateOnly(2030, 3, 3),
            PaymentMethod = PaymentMethod.Card,
        });

        var rooms = await (await _client.GetAsync("/api/rooms/availability?startDate=2030-03-01&nights=2"))
            .ReadAsync<List<RoomResponse>>();

        Assert.DoesNotContain(rooms, r => r.Id == 2);
        Assert.Contains(rooms, r => r.Id != 2);
    }

    [Fact]
    public async Task Availability_with_zero_nights_returns_400()
    {
        var response = await _client.GetAsync("/api/rooms/availability?startDate=2030-01-01&nights=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
