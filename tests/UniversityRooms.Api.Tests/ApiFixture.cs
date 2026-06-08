using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using UniversityRooms.Api.Services;

namespace UniversityRooms.Api.Tests;

/// <summary>
/// Boots the API in-process with its seeded in-memory database and hands tests
/// an <see cref="HttpClient"/>. Shared across all test classes so the database
/// is seeded exactly once.
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>
{
    public HttpClient Client => CreateClient();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Captures emails the app sends during a test run.</summary>
    public TestEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Keep test output readable — silence the framework/EF Core log spam.
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
        });
    }
}

/// <summary>
/// All tests live in one collection so they run sequentially against the single
/// shared database (xUnit parallelises across collections, not within one).
/// </summary>
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;

internal static class HttpExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(ApiFixture.Json);
        Assert.NotNull(value);
        return value!;
    }
}
