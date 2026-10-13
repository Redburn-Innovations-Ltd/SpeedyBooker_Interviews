using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Services;

/// <summary>
/// Releases bookings whose payment hasn't been captured within the hold window,
/// so an abandoned checkout doesn't keep a room off the market.
/// </summary>
public class BookingHoldService(AppDbContext db)
{
    /// <summary>How long a new booking holds its room while waiting for payment.</summary>
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Cancel every pending booking created before <paramref name="cutoffUtc"/>,
    /// freeing its room for other guests. Returns the number of holds released.
    /// </summary>
    public async Task<int> ReleaseExpiredHoldsAsync(DateTime cutoffUtc, CancellationToken ct = default)
    {
        var expired = await db.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.CreatedAtUtc < cutoffUtc)
            .ToListAsync(ct);

        foreach (var booking in expired)
            booking.Status = BookingStatus.Cancelled;

        await db.SaveChangesAsync(ct);
        return expired.Count;
    }
}

/// <summary>
/// Background job that releases expired holds once a minute.
/// </summary>
public class BookingHoldExpiryJob(IServiceScopeFactory scopeFactory, ILogger<BookingHoldExpiryJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var holds = scope.ServiceProvider.GetRequiredService<BookingHoldService>();

            var cutoff = DateTime.Now - BookingHoldService.HoldDuration;
            var released = await holds.ReleaseExpiredHoldsAsync(cutoff, stoppingToken);

            if (released > 0)
                logger.LogInformation("Released {Count} expired booking holds", released);
        }
    }
}
