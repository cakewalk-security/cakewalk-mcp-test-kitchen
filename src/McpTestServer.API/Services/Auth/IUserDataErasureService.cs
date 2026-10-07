namespace McpTestServer.API.Services.Auth;

public interface IUserDataErasureService
{
    Task EraseAllAsync(string email, CancellationToken cancellationToken);
}
