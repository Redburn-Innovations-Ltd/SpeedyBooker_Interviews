namespace UniversityRooms.Api.Models;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}

/// <summary>
/// A reservation of a <see cref="Room"/> for a window of time.
/// </summary>
/// <remarks>
/// Bookings are identified only by the booker's email address — there is no
/// User/Account entity in the system. This keeps the demo small but means a
/// "user" has no profile, history view, or identity beyond a string.
/// </remarks>
public class Booking
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>The person who made the booking. The closest thing to a user.</summary>
    public required string ContactEmail { get; set; }

    /// <summary>First night of the stay.</summary>
    public DateOnly CheckInDate { get; set; }

    /// <summary>The morning the room is vacated; the stay covers the nights in between.</summary>
    public DateOnly CheckOutDate { get; set; }

    public BookingStatus Status { get; set; }

    /// <summary>Total price of the booking in GBP, derived from the room's nightly rate and the number of nights.</summary>
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<Payment> Payments { get; set; } = [];
}
