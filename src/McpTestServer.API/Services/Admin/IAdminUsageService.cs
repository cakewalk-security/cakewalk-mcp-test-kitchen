using McpTestServer.API.Controllers.Management.Models;

namespace McpTestServer.API.Services.Admin;

public interface IAdminUsageService
{
    Task<AdminUsageResponse> GetUsageAsync(CancellationToken cancellationToken);
}
