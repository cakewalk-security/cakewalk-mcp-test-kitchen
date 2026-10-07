namespace McpTestServer.API.Services.Auth;

public interface IUserMcpPatValidator
{
    Task<McpPatValidationResult> ValidateAsync(string providedToken, CancellationToken cancellationToken);
}
