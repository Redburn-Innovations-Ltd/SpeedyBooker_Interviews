using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vendor>(e =>
        {
            e.Property(v => v.Name).HasMaxLength(200);
            e.Property(v => v.ContactEmail).HasMaxLength(320);
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(200);
            e.Property(r => r.Building).HasMaxLength(200);
            e.Property(r => r.NightlyRate).HasPrecision(18, 2);

            e.HasOne(r => r.Vendor)
                .WithMany(v => v.Rooms)
                .HasForeignKey(r => r.VendorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.ContactEmail).HasMaxLength(320);
            e.Property(b => b.TotalPrice).HasPrecision(18, 2);
            e.Property(b => b.Status).HasConversion<string>();

            e.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(b => new { b.RoomId, b.CheckInDate });
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Status).HasConversion<string>();
            e.Property(p => p.Method).HasConversion<string>();

            e.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
