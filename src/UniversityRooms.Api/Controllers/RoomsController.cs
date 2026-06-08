using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// List bookable rooms, optionally filtered by minimum capacity or vendor.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetAll(
        [FromQuery] int? minCapacity,
        [FromQuery] int? vendorId)
    {
        var query = db.Rooms.Include(r => r.Vendor).AsQueryable();

        if (minCapacity is not null)
            query = query.Where(r => r.Capacity >= minCapacity);

        if (vendorId is not null)
            query = query.Where(r => r.VendorId == vendorId);

        var rooms = await query.OrderBy(r => r.Building).ThenBy(r => r.Name).ToListAsync();
        return Ok(rooms.Select(RoomResponse.From));
    }

    /// <summary>
    /// Find rooms that are free for a stay starting on <paramref name="startDate"/>
    /// for the given number of <paramref name="nights"/>.
    /// </summary>
    [HttpGet("availability")]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetAvailability(
        [FromQuery] DateOnly startDate,
        [FromQuery] int nights)
    {
        if (nights < 1)
            return BadRequest("nights must be at least 1.");

        var checkIn = startDate;
        var checkOut = startDate.AddDays(nights);

        // Pull back every room, then check each one's bookings one at a time.
        var rooms = await db.Rooms.Include(r => r.Vendor).ToListAsync();

        var available = new List<RoomResponse>();
        foreach (var room in rooms)
        {
            // A fresh query per room, then the overlap test runs in memory.
            var bookings = await db.Bookings
                .Where(b => b.RoomId == room.Id)
                .ToListAsync();

            var alreadyBooked = bookings.Any(b =>
                b.Status != BookingStatus.Cancelled &&
                checkIn < b.CheckOutDate &&
                checkOut > b.CheckInDate);

            if (!alreadyBooked)
                available.Add(RoomResponse.From(room));
        }

        return Ok(available.OrderBy(r => r.Building).ThenBy(r => r.Name));
    }

    /// <summary>
    /// Search for available rooms for a stay, with optional capacity/price
    /// filters, sorted cheapest first and returned a page at a time.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedRooms>> Search(
        [FromQuery] DateOnly startDate,
        [FromQuery] int nights,
        [FromQuery] int? minCapacity,
        [FromQuery] decimal? maxNightlyRate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (nights < 1)
            return BadRequest("nights must be at least 1.");

        var checkIn = startDate;
        var checkOut = startDate.AddDays(nights);

        var rooms = await db.Rooms.Include(r => r.Vendor).ToListAsync();

        var available = new List<RoomResponse>();
        foreach (var room in rooms)
        {
            if (minCapacity is not null && room.Capacity < minCapacity)
                continue;
            if (maxNightlyRate is not null && room.NightlyRate > maxNightlyRate)
                continue;

            var bookings = await db.Bookings
                .Where(b => b.RoomId == room.Id)
                .ToListAsync();

            var alreadyBooked = bookings.Any(b =>
                b.Status != BookingStatus.Cancelled &&
                checkIn < b.CheckOutDate &&
                checkOut > b.CheckInDate);

            if (!alreadyBooked)
                available.Add(RoomResponse.From(room));
        }

        // Return the requested page, cheapest first.
        var items = available
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(r => r.NightlyRate)
            .ToList();

        return Ok(new PagedRooms(page, pageSize, items));
    }

    /// <summary>Get a single room.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(int id)
    {
        var room = await db.Rooms
            .Include(r => r.Vendor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
            return NotFound();

        return Ok(RoomResponse.From(room));
    }

    /// <summary>List bookings for a room.</summary>
    [HttpGet("{id:int}/bookings")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookings(int id)
    {
        if (!await db.Rooms.AnyAsync(r => r.Id == id))
            return NotFound();

        var bookings = await db.Bookings
            .Include(b => b.Room)!.ThenInclude(r => r!.Vendor)
            .Where(b => b.RoomId == id)
            .OrderBy(b => b.CheckInDate)
            .ToListAsync();

        return Ok(bookings.Select(BookingResponse.From));
    }
}
