using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Dtos;

public record VendorResponse(int Id, string Name, string Address, string ContactEmail, int RoomCount)
{
    public static VendorResponse From(Vendor v) =>
        new(v.Id, v.Name, v.Address, v.ContactEmail, v.Rooms.Count);
}

public record RoomResponse(
    int Id, int VendorId, string VendorName, string Name, string Building, int Capacity, decimal NightlyRate)
{
    public static RoomResponse From(Room r) =>
        new(r.Id, r.VendorId, r.Vendor?.Name ?? "", r.Name, r.Building, r.Capacity, r.NightlyRate);
}

public record PaymentResponse(
    int Id, int BookingId, decimal Amount, string Status, string Method, DateTime CreatedAtUtc)
{
    public static PaymentResponse From(Payment p) =>
        new(p.Id, p.BookingId, p.Amount, p.Status.ToString(), p.Method.ToString(), p.CreatedAtUtc);
}
