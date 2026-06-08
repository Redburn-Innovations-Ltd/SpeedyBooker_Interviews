using UniversityRooms.Api.Services;

namespace UniversityRooms.Api.Tests;

/// <summary>Records the emails the app tries to send so tests can assert on them.</summary>
public sealed class TestEmailSender : IEmailSender
{
    public List<SentEmail> Sent { get; } = [];

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        Sent.Add(new SentEmail(to, subject, body));
        return Task.CompletedTask;
    }
}

public record SentEmail(string To, string Subject, string Body);
