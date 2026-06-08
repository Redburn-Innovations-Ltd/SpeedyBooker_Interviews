using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Services;

/// <summary>
/// The outcome of attempting to create a booking. Lets the controller map a
/// failure to the right HTTP status without throwing for expected cases.
/// </summary>
public enum BookingError
{
    None,
    RoomNotFound,
    InvalidTimeRange,
    Overlap,
}

public record CreateBookingResult(Booking? Booking, BookingError Error)
{
    public bool Success => Error == BookingError.None;
}

public interface IBookingService
{
    Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken ct = default);
}

/// <summary>
/// Holds the booking domain logic: validating the time range, checking the room
/// is free, pricing the booking, and recording the initial payment.
/// </summary>
public class BookingService(AppDbContext db, IEmailSender email) : IBookingService
{
    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        if (request.CheckOutDate <= request.CheckInDate)
            return new CreateBookingResult(null, BookingError.InvalidTimeRange);

        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        if (room is null)
            return new CreateBookingResult(null, BookingError.RoomNotFound);

        // Stays use half-open [check-in, check-out) ranges, so one guest can check
        // out on the same day another checks in without it counting as an overlap.
        var overlaps = await db.Bookings.AnyAsync(b =>
            b.RoomId == request.RoomId &&
            b.Status != BookingStatus.Cancelled &&
            request.CheckInDate < b.CheckOutDate &&
            request.CheckOutDate > b.CheckInDate, ct);

        if (overlaps)
            return new CreateBookingResult(null, BookingError.Overlap);

        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        var total = nights * room.NightlyRate;

        var booking = new Booking
        {
            RoomId = room.Id,
            ContactEmail = request.ContactEmail,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Status = BookingStatus.Confirmed,
            TotalPrice = total,
            CreatedAtUtc = DateTime.UtcNow,
            Payments =
            [
                new Payment
                {
                    Amount = total,
                    Status = PaymentStatus.Pending,
                    Method = request.PaymentMethod,
                    CreatedAtUtc = DateTime.UtcNow,
                },
            ],
        };

        db.Bookings.Add(booking);

        // Let the guest know their stay is confirmed.
        var body =
            $"Hi,\n\nYour stay at {booking.Room?.Name}, {booking.Room?.Vendor?.Name}, " +
            $"from {booking.CheckInDate:d MMM yyyy} to {booking.CheckOutDate:d MMM yyyy} is confirmed.\n" +
            $"Total: £{booking.TotalPrice}.\n\nThanks for booking with us.";
        await email.SendAsync(booking.ContactEmail, "Your booking is confirmed", body, ct);

        await db.SaveChangesAsync(ct);

        // Re-load navigation properties for the response.
        await db.Entry(booking).Reference(b => b.Room).LoadAsync(ct);
        await db.Entry(booking.Room!).Reference(r => r.Vendor).LoadAsync(ct);

        return new CreateBookingResult(booking, BookingError.None);
    }
}
