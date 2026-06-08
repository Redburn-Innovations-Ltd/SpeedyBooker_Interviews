using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace UniversityRooms.Api.Specs.Support;

/// <summary>
/// Boots the API once for the whole test run with its seeded in-memory database.
/// </summary>
internal sealed class SpecApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureLogging(logging => logging.ClearProviders());
}

/// <summary>
/// Per-scenario state shared between step definitions (Reqnroll injects one
/// instance per scenario via its container). Holds the HTTP client and the most
/// recent response so steps can act on it.
/// </summary>
public sealed class ApiContext : IDisposable
{
    private static readonly SpecApiFactory Factory = new();

    public HttpClient Client { get; } = Factory.CreateClient();

    public HttpResponseMessage? LastResponse { get; set; }
    public string LastBody { get; set; } = "";

    /// <summary>Id pulled from the most recent response body, for use in later request paths.</summary>
    public int? LastId { get; set; }

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Replaces a <c>{id}</c> placeholder in a path with the last captured id.</summary>
    public string ResolvePath(string path) =>
        LastId is null ? path : path.Replace("{id}", LastId.Value.ToString());

    public void Dispose() => Client.Dispose();
}
