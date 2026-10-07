using McpTestServer.API.Options;
using Microsoft.Extensions.Options;
using Resend;

namespace McpTestServer.API.Services.Feedback;

public sealed class FeedbackService(
    IResend resend,
    IOptions<FeedbackOptions> feedbackOptions,
    IConfiguration configuration,
    ILogger<FeedbackService> logger) : IFeedbackService
{
    public async Task SendAsync(string? senderEmail, string message, CancellationToken cancellationToken)
    {
        var apiToken = configuration[FeedbackOptions.ResendApiTokenEnv];
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            logger.LogWarning("[{ServiceName}] Feedback email skipped because Resend API token is not configured.", nameof(FeedbackService));
            return;
        }

        var options = feedbackOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Recipient))
        {
            logger.LogWarning("[{ServiceName}] Feedback email skipped because no feedback recipient is configured.", nameof(FeedbackService));
            return;
        }

        var emailMessage = new EmailMessage
        {
            From = options.FromAddress,
            Subject = FeedbackOptions.EmailSubject,
            TextBody = message,
        };

        emailMessage.To.Add(options.Recipient);

        if (!string.IsNullOrWhiteSpace(senderEmail))
        {
            emailMessage.ReplyTo ??= [];
            emailMessage.ReplyTo.Add(senderEmail);
        }

        await resend.EmailSendAsync(emailMessage, cancellationToken);
        logger.LogInformation("[{ServiceName}] Feedback email sent successfully.", nameof(FeedbackService));
    }
}
