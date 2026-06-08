# University Rooms API

A small REST API for a university accommodation-booking platform, in the spirit of
[universityrooms.com](https://www.universityrooms.com/), where the public can book
college guest rooms by the night. Colleges (*vendors*) list their *rooms*; people
make *bookings* for a stay and a *payment* is recorded for each one.

Built with ASP.NET Core (.NET 10), EF Core, and an in-memory SQLite database
that is seeded with sample data on startup. There is no authentication — it's an
API-only demo you drive through Swagger UI.

## Running it

```bash
dotnet run --project src/UniversityRooms.Api
```

Then open **<http://localhost:5216/swagger>** (the http profile opens it for you).
The root URL redirects there too.

The database is rebuilt and re-seeded every time the app starts, so you can
experiment freely and restart to get back to a clean slate.

## Domain model

```
Vendor ──< Room ──< Booking ──< Payment
```

| Entity    | What it is                                                                 |
|-----------|----------------------------------------------------------------------------|
| `Vendor`  | A college that owns and lets out guest rooms on the platform.               |
| `Room`    | A bookable guest room belonging to a vendor. Has a capacity and a nightly rate. |
| `Booking` | A reservation of a room for one or more whole nights, identified by the booker's email. |
| `Payment` | A payment recorded against a booking. Funds settle with the room's vendor.  |

## Endpoints

| Method & route                     | Description                                        |
|------------------------------------|----------------------------------------------------|
| `GET    /api/vendors`              | List vendors.                                      |
| `GET    /api/vendors/{id}`         | Get a vendor.                                       |
| `GET    /api/vendors/{id}/rooms`   | List a vendor's rooms.                              |
| `GET    /api/rooms`                | List rooms. Filters: `minCapacity`, `vendorId`.     |
| `GET    /api/rooms/availability`   | Rooms free for a stay. Query: `startDate`, `nights`. |
| `GET    /api/rooms/{id}`           | Get a room.                                         |
| `GET    /api/rooms/{id}/bookings`  | List a room's bookings.                             |
| `GET    /api/bookings`             | List bookings. Filter: `email`.                     |
| `GET    /api/bookings/{id}`        | Get a booking.                                      |
| `POST   /api/bookings`             | Create a booking (checks availability, prices it, records a pending payment). |
| `POST   /api/bookings/{id}/cancel` | Cancel a booking.                                   |
| `GET    /api/payments`             | List payments. Filter: `bookingId`.                 |
| `GET    /api/payments/{id}`        | Get a payment.                                      |
| `POST   /api/payments/{id}/capture`| Mark a pending payment as paid.                     |

### Creating a booking

Rooms are booked by the night. `checkInDate` is the first night and
`checkOutDate` is the morning the room is vacated, so the booking below is a
**one-night stay**:

```bash
curl -X POST http://localhost:5216/api/bookings \
  -H 'Content-Type: application/json' \
  -d '{
        "roomId": 3,
        "contactEmail": "j.murphy@gmail.com",
        "checkInDate": "2026-06-13",
        "checkOutDate": "2026-06-14",
        "paymentMethod": "Card"
      }'
```

The price is `nightlyRate × nights`. A booking that overlaps an existing
(non-cancelled) booking for the same room is rejected with `409 Conflict` —
though one guest can check out on the same day another checks in.

## Running the tests

```bash
dotnet test
```

To run them with a code-coverage summary printed to the console:

```bash
dotnet test /p:CollectCoverage=true --tl:off
```

This prints a per-module Line/Branch/Method table and writes a Cobertura report
(`coverage.cobertura.xml`) under the test project. The module filter and report
format are set in the test project's `.csproj`, so no extra arguments are needed.

> `--tl:off` disables .NET's terminal logger, which otherwise swallows coverlet's
> summary table when running interactively. The `coverage.cobertura.xml` report is
> written either way, so you can drop the flag in CI.

### BDD / Cucumber specs

`tests/UniversityRooms.Api.Specs/` contains Gherkin feature files driven with
[Reqnroll](https://reqnroll.net/) (the maintained successor to SpecFlow). They
exercise the booking flow as plain-English steps against the real HTTP endpoints:

```gherkin
When I make a POST request to "/api/bookings" with body:
  """
  { "roomId": 3, "contactEmail": "cucumber@example.com",
    "startUtc": "2028-03-01T09:00:00Z", "endUtc": "2028-03-01T11:00:00Z" }
  """
Then the response status code should be 201
And the response field "status" should be "Confirmed"
```

`dotnet test` runs them alongside the other tests; to run only the specs:

```bash
dotnet test tests/UniversityRooms.Api.Specs --tl:off
```

## Project layout

```
src/UniversityRooms.Api/
  Controllers/   HTTP endpoints
  Services/      BookingService — availability, pricing, payment creation
  Models/        EF Core entities (Vendor, Room, Booking, Payment)
  Dtos/          Request/response shapes
  Data/          AppDbContext and the startup seeder
  Program.cs     Composition root, SQLite + Swagger wiring
tests/UniversityRooms.Api.Tests/   Integration tests over the running API
tests/UniversityRooms.Api.Specs/   Reqnroll (Cucumber) feature specs
```

## Design notes & simplifications

This is a teaching/demo codebase, deliberately kept small. A few things are
simpler than a production system would be:

- **In-memory database.** All data is lost on restart; there are no migrations.
- **No authentication or authorisation.** Every endpoint is open.
- **No payment integration.** "Capturing" a payment just flips a status field.
