using System.Text;
using System.Text.Json;
using Reqnroll;
using UniversityRooms.Api.Specs.Support;
using Xunit;

namespace UniversityRooms.Api.Specs.Steps;

/// <summary>
/// Generic REST step definitions: make requests to the API and assert on the
/// response. Reqnroll constructs this per scenario and injects the shared
/// <see cref="ApiContext"/>.
/// </summary>
[Binding]
public class RestSteps(ApiContext api)
{
    [When(@"I make a GET request to ""(.*)""")]
    public async Task WhenIMakeAGetRequestTo(string path)
    {
        api.LastResponse = await api.Client.GetAsync(api.ResolvePath(path));
        await CaptureBodyAsync();
    }

    [When(@"I make a POST request to ""(.*)""")]
    public async Task WhenIMakeAPostRequestTo(string path)
    {
        api.LastResponse = await api.Client.PostAsync(api.ResolvePath(path), content: null);
        await CaptureBodyAsync();
    }

    [When(@"I make a POST request to ""(.*)"" with body:")]
    public async Task WhenIMakeAPostRequestToWithBody(string path, string body)
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        api.LastResponse = await api.Client.PostAsync(api.ResolvePath(path), content);
        await CaptureBodyAsync();
    }

    [Then(@"the response status code should be (\d+)")]
    public void ThenTheResponseStatusCodeShouldBe(int statusCode)
    {
        Assert.NotNull(api.LastResponse);
        Assert.Equal(statusCode, (int)api.LastResponse!.StatusCode);
    }

    [Then(@"the response should contain ""(.*)""")]
    public void ThenTheResponseShouldContain(string text)
    {
        Assert.Contains(text, api.LastBody);
    }

    [Then(@"the response should not contain ""(.*)""")]
    public void ThenTheResponseShouldNotContain(string text)
    {
        Assert.DoesNotContain(text, api.LastBody);
    }

    [Then(@"the response field ""(.*)"" should be ""(.*)""")]
    public void ThenTheResponseFieldShouldBe(string field, string expected)
    {
        using var doc = JsonDocument.Parse(api.LastBody);
        var actual = doc.RootElement.GetProperty(field).ToString();
        Assert.Equal(expected, actual);
    }

    [Then(@"I remember the booking id")]
    public void ThenIRememberTheBookingId()
    {
        using var doc = JsonDocument.Parse(api.LastBody);
        api.LastId = doc.RootElement.GetProperty("id").GetInt32();
    }

    private async Task CaptureBodyAsync()
    {
        api.LastBody = await api.LastResponse!.Content.ReadAsStringAsync();
    }
}
