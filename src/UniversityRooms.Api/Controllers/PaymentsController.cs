using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Dtos;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(AppDbContext db) : ControllerBase
{
    /// <summary>List payments, optionally filtered by booking.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetAll([FromQuery] int? bookingId)
    {
        var query = db.Payments.AsQueryable();

        if (bookingId is not null)
            query = query.Where(p => p.BookingId == bookingId);

        var payments = await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync();
        return Ok(payments.Select(PaymentResponse.From));
    }

    /// <summary>Get a single payment.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentResponse>> GetById(int id)
    {
        var payment = await db.Payments.FindAsync(id);
        if (payment is null)
            return NotFound();

        return Ok(PaymentResponse.From(payment));
    }

    /// <summary>
    /// Mark a pending payment as paid. Real integrations would reconcile against
    /// a payment provider; here we simply flip the status.
    /// </summary>
    [HttpPost("{id:int}/capture")]
    public async Task<ActionResult<PaymentResponse>> Capture(int id)
    {
        var payment = await db.Payments.FindAsync(id);
        if (payment is null)
            return NotFound();

        payment.Status = PaymentStatus.Paid;
        await db.SaveChangesAsync();

        return Ok(PaymentResponse.From(payment));
    }
}
