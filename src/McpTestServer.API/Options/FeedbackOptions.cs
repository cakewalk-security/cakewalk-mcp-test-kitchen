namespace McpTestServer.API.Options;

public sealed class FeedbackOptions
{
    public const string ResendApiTokenEnv = "RESEND_APITOKEN";

    public const string FeedbackRecipientEnv = "MCP_TEST_SERVER_FEEDBACK_RECIPIENT";

    public const string FeedbackFromEnv = "MCP_TEST_SERVER_FEEDBACK_FROM";

    public const string DefaultFromAddress = "MCP Test Kitchen <onboarding@resend.dev>";

    public const string EmailSubject = "MCP Test Kitchen Feedback";

    public const int MaxMessageLength = 2000;

    public string Recipient { get; set; } = string.Empty;

    public string FromAddress { get; set; } = DefaultFromAddress;
}
