namespace UniversityRooms.Api.Models;

/// <summary>
/// A college (or hall) that owns and lets out guest rooms on the platform.
/// Payments for a booking are settled with the vendor that owns the booked room.
/// </summary>
public class Vendor
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Address line shown to users browsing rooms.</summary>
    public required string Address { get; set; }

    /// <summary>Mailbox the vendor uses for booking notifications.</summary>
    public required string ContactEmail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<Room> Rooms { get; set; } = [];
}
