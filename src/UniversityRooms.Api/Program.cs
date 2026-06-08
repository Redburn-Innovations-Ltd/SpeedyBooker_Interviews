using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UniversityRooms.Api.Data;
using UniversityRooms.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// In-memory SQLite. The database lives only as long as at least one connection
// to it is open, so we hold a single connection open for the app's lifetime and
// point EF Core at the same shared in-memory database.
var keepAliveConnection = new SqliteConnection("DataSource=UniversityRooms;Mode=Memory;Cache=Shared");
keepAliveConnection.Open();

builder.Services.AddSingleton(keepAliveConnection);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("DataSource=UniversityRooms;Mode=Memory;Cache=Shared"));

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Serialise/accept enums as their string names (e.g. "Confirmed", "Card")
        // so request and response bodies read the same in Swagger.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "University Rooms API", Version = "v1" });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "UniversityRooms.Api.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Create the schema and seed sample data at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "University Rooms API v1");
    options.RoutePrefix = "swagger";
});

// Send the root straight to Swagger UI so there's something to look at.
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

app.Run();

// Exposed so the integration test host (WebApplicationFactory) can boot the app.
public partial class Program;
