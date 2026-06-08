using System.Net;
using System.Net.Http.Json;
using UniversityRooms.Api.Dtos;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class VendorsTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task GetAll_returns_seeded_vendors()
    {
        var vendors = await (await _client.GetAsync("/api/vendors"))
            .ReadAsync<List<VendorResponse>>();

        Assert.True(vendors.Count >= 3);
        Assert.Contains(vendors, v => v.Name == "Balliol College");
        Assert.All(vendors, v => Assert.True(v.RoomCount >= 0));
    }

    [Fact]
    public async Task GetById_returns_vendor()
    {
        var vendor = await (await _client.GetAsync("/api/vendors/1"))
            .ReadAsync<VendorResponse>();

        Assert.Equal(1, vendor.Id);
        Assert.False(string.IsNullOrWhiteSpace(vendor.ContactEmail));
        Assert.True(vendor.RoomCount > 0);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        var response = await _client.GetAsync("/api/vendors/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRooms_returns_the_vendors_rooms()
    {
        var rooms = await (await _client.GetAsync("/api/vendors/1/rooms"))
            .ReadAsync<List<RoomResponse>>();

        Assert.NotEmpty(rooms);
        Assert.All(rooms, r => Assert.Equal(1, r.VendorId));
    }

    [Fact]
    public async Task GetRooms_for_unknown_vendor_returns_404()
    {
        var response = await _client.GetAsync("/api/vendors/99999/rooms");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
