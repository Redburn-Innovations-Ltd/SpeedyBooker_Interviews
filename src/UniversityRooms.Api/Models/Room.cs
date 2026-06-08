namespace UniversityRooms.Api.Models;

/// <summary>
/// A bookable room belonging to a <see cref="Vendor"/>.
/// </summary>
public class Room
{
    public int Id { get; set; }

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public required string Name { get; set; }

    /// <summary>Building or staircase the room sits in, e.g. "Jowett Walk Building".</summary>
    public required string Building { get; set; }

    public int Capacity { get; set; }

    /// <summary>Price charged per night booked, in GBP.</summary>
    public decimal NightlyRate { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<Booking> Bookings { get; set; } = [];
}
