namespace UniversityRooms.Api.Services;

/// <summary>
/// Sends transactional emails (booking confirmations, etc.).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Development email sender that just writes the message to the logs. A real
/// deployment would swap in an SMTP or provider-backed implementation.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("Email to {To} — {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
