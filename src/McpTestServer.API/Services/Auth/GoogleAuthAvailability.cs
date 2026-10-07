using McpTestServer.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Services.Auth;

public interface IGoogleAuthAvailability
{
    bool IsConfigured { get; }
}

public sealed class GoogleAuthAvailability(IOptions<GoogleCredentialsOptions> options) : IGoogleAuthAvailability
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.Value.INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID);
}
