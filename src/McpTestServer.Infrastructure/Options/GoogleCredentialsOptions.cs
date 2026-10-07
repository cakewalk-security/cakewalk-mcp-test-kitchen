namespace McpTestServer.Infrastructure.Options;

public sealed class GoogleCredentialsOptions
{
    public string INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_SECRET { get; set; } = string.Empty;

    public string INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID { get; set; } = string.Empty;
}
