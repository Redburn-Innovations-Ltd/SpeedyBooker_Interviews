using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;
using UniversityRooms.Api.Services;

namespace UniversityRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext db, IBookingService bookingService) : ControllerBase
{
    /// <summary>
    /// List bookings, optionally filtered by the booker's email address.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetAll([FromQuery] string? email)
    {
        var query = db.Bookings
            .Include(b => b.Room)!.ThenInclude(r => r!.Vendor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(b => b.ContactEmail == email);

        var bookings = await query.OrderByDescending(b => b.CheckInDate).ToListAsync();
        return Ok(bookings.Select(BookingResponse.From));
    }

    /// <summary>Get a single booking.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(int id)
    {
        var booking = await db.Bookings
            .Include(b => b.Room)!.ThenInclude(r => r!.Vendor)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound();

        return Ok(BookingResponse.From(booking));
    }

    /// <summary>
    /// Create a booking for a room. The room must be free for the requested
    /// window; a pending payment is recorded alongside the booking.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request)
    {
        var result = await bookingService.CreateAsync(request);

        if (!result.Success)
        {
            return result.Error switch
            {
                BookingError.RoomNotFound => NotFound($"Room {request.RoomId} was not found."),
                BookingError.InvalidTimeRange => BadRequest("CheckOutDate must be after CheckInDate (stays are at least one night)."),
                BookingError.Overlap => Conflict("The room is already booked for part of that time."),
                _ => BadRequest(),
            };
        }

        var dto = BookingResponse.From(result.Booking!);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Cancel a booking.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null)
            return NotFound();

        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();

        return NoContent();
    }
}
