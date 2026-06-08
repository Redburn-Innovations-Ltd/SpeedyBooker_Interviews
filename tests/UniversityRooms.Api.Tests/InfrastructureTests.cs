using System.Net;

namespace UniversityRooms.Api.Tests;

[Collection("api")]
public class InfrastructureTests(ApiFixture fixture)
{
    [Fact]
    public async Task Root_redirects_to_swagger()
    {
        var client = fixture.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Swagger_document_is_served()
    {
        var response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("University Rooms API", body);
    }
}
