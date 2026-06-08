using System.ComponentModel.DataAnnotations;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Dtos;

public class CreateBookingRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required, EmailAddress]
    public required string ContactEmail { get; set; }

    /// <summary>First night of the stay, e.g. 2026-06-13.</summary>
    [Required]
    public DateOnly CheckInDate { get; set; }

    /// <summary>Day the room is vacated, e.g. 2026-06-14 for a one-night stay.</summary>
    [Required]
    public DateOnly CheckOutDate { get; set; }

    /// <summary>How the booker intends to pay. A matching payment is created with the booking.</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
}

public class BookingResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Nights { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public static BookingResponse From(Booking b) => new()
    {
        Id = b.Id,
        RoomId = b.RoomId,
        RoomName = b.Room?.Name ?? "",
        VendorName = b.Room?.Vendor?.Name ?? "",
        ContactEmail = b.ContactEmail,
        CheckInDate = b.CheckInDate,
        CheckOutDate = b.CheckOutDate,
        Nights = b.CheckOutDate.DayNumber - b.CheckInDate.DayNumber,
        Status = b.Status.ToString(),
        TotalPrice = b.TotalPrice,
        CreatedAtUtc = b.CreatedAtUtc,
    };
}
