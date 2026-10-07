namespace McpTestServer.API.Services.Feedback;

public interface IFeedbackService
{
    Task SendAsync(string? senderEmail, string message, CancellationToken cancellationToken);
}
