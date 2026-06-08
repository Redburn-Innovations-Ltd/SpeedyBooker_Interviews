using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Models;

namespace UniversityRooms.Api.Data;

/// <summary>
/// Populates the in-memory database with a small, realistic data set so the API
/// returns something interesting straight away. Runs once at startup.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Vendors.Any())
            return;

        // Anchor sample stays around "today" so they show as upcoming.
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var createdAt = DateTime.UtcNow.Date.AddDays(-30);

        var balliol = new Vendor
        {
            Name = "Balliol College",
            Address = "Broad Street, Oxford OX1 3BJ",
            ContactEmail = "accommodation@balliol.example.ac.uk",
            CreatedAtUtc = createdAt,
            Rooms =
            [
                new Room { Name = "Standard Single", Building = "Staircase XII", Capacity = 1, NightlyRate = 65m, CreatedAtUtc = createdAt },
                new Room { Name = "En-suite Double", Building = "Staircase XII", Capacity = 2, NightlyRate = 95m, CreatedAtUtc = createdAt },
                new Room { Name = "Twin Room", Building = "Jowett Walk Building", Capacity = 2, NightlyRate = 110m, CreatedAtUtc = createdAt },
            ],
        };

        var magdalen = new Vendor
        {
            Name = "Magdalen College",
            Address = "High Street, Oxford OX1 4AU",
            ContactEmail = "stay@magdalen.example.ac.uk",
            CreatedAtUtc = createdAt,
            Rooms =
            [
                new Room { Name = "Garden View Single", Building = "New Buildings", Capacity = 1, NightlyRate = 80m, CreatedAtUtc = createdAt },
                new Room { Name = "Riverside Double (en-suite)", Building = "Waynflete Building", Capacity = 2, NightlyRate = 130m, CreatedAtUtc = createdAt },
            ],
        };

        var stAnnes = new Vendor
        {
            Name = "St Anne's College",
            Address = "Woodstock Road, Oxford OX2 6HS",
            ContactEmail = "rooms@st-annes.example.ac.uk",
            CreatedAtUtc = createdAt,
            Rooms =
            [
                new Room { Name = "Single (shared bathroom)", Building = "Bevington Road", Capacity = 1, NightlyRate = 55m, CreatedAtUtc = createdAt },
                new Room { Name = "Family Room", Building = "Ruth Deech Building", Capacity = 4, NightlyRate = 150m, CreatedAtUtc = createdAt },
            ],
        };

        db.Vendors.AddRange(balliol, magdalen, stAnnes);
        db.SaveChanges();

        var balliolSingle = balliol.Rooms[0];
        var magdalenSingle = magdalen.Rooms[0];
        var stAnnesSingle = stAnnes.Rooms[0];

        var bookings = new List<Booking>
        {
            // A two-night stay.
            MakeBooking(balliolSingle, "g.fischer@gmail.com",
                today.AddDays(3), today.AddDays(5),
                BookingStatus.Confirmed, createdAt),

            // A one-night stay.
            MakeBooking(magdalenSingle, "j.murphy@gmail.com",
                today.AddDays(5), today.AddDays(6),
                BookingStatus.Confirmed, createdAt),

            // A three-night stay, not yet paid.
            MakeBooking(stAnnesSingle, "h.nakamura@outlook.com",
                today.AddDays(10), today.AddDays(13),
                BookingStatus.Pending, createdAt),

            // A past, cancelled one-night stay.
            MakeBooking(balliolSingle, "d.okafor@gmail.com",
                today.AddDays(-2), today.AddDays(-1),
                BookingStatus.Cancelled, createdAt),
        };

        db.Bookings.AddRange(bookings);
        db.SaveChanges();

        db.Payments.AddRange(
            new Payment { BookingId = bookings[0].Id, Amount = bookings[0].TotalPrice, Status = PaymentStatus.Paid, Method = PaymentMethod.Card, CreatedAtUtc = createdAt },
            new Payment { BookingId = bookings[1].Id, Amount = bookings[1].TotalPrice, Status = PaymentStatus.Paid, Method = PaymentMethod.Invoice, CreatedAtUtc = createdAt },
            new Payment { BookingId = bookings[2].Id, Amount = bookings[2].TotalPrice, Status = PaymentStatus.Pending, Method = PaymentMethod.BankTransfer, CreatedAtUtc = createdAt },
            new Payment { BookingId = bookings[3].Id, Amount = bookings[3].TotalPrice, Status = PaymentStatus.Refunded, Method = PaymentMethod.Card, CreatedAtUtc = createdAt }
        );
        db.SaveChanges();
    }

    private static Booking MakeBooking(Room room, string email, DateOnly checkIn, DateOnly checkOut,
        BookingStatus status, DateTime createdAt)
    {
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        return new Booking
        {
            RoomId = room.Id,
            ContactEmail = email,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Status = status,
            TotalPrice = nights * room.NightlyRate,
            CreatedAtUtc = createdAt,
        };
    }
}
