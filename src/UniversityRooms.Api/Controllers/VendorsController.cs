using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Dtos;

namespace UniversityRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController(AppDbContext db) : ControllerBase
{
    /// <summary>List all vendors (room providers) on the platform.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorResponse>>> GetAll()
    {
        var vendors = await db.Vendors
            .Include(v => v.Rooms)
            .OrderBy(v => v.Name)
            .ToListAsync();

        return Ok(vendors.Select(VendorResponse.From));
    }

    /// <summary>Get a single vendor and its rooms.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendorResponse>> GetById(int id)
    {
        var vendor = await db.Vendors
            .Include(v => v.Rooms)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vendor is null)
            return NotFound();

        return Ok(VendorResponse.From(vendor));
    }

    /// <summary>List the rooms belonging to a vendor.</summary>
    [HttpGet("{id:int}/rooms")]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetRooms(int id)
    {
        if (!await db.Vendors.AnyAsync(v => v.Id == id))
            return NotFound();

        var rooms = await db.Rooms
            .Include(r => r.Vendor)
            .Where(r => r.VendorId == id)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Ok(rooms.Select(RoomResponse.From));
    }
}
