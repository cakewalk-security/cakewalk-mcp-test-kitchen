namespace McpTestServer.API.Services.Auth;

public interface IUserMcpPatService
{
    Task<string> GetOrCreatePatAsync(string email, CancellationToken cancellationToken);

    Task<string> RegeneratePatAsync(string email, CancellationToken cancellationToken);
}
