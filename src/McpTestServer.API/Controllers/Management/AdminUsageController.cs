using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Extensions;
using McpTestServer.API.Options;
using McpTestServer.API.Services.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Controllers.Management;

[ApiController]
[Route("api/management/admin")]
public sealed class AdminUsageController(
    IAdminUsageService adminUsageService,
    IOptions<AdminAccessOptions> adminAccessOptions) : ControllerBase
{
    [HttpGet("usage")]
    public async Task<ActionResult<AdminUsageResponse>> GetUsage(CancellationToken cancellationToken)
    {
        if (!User.CanAccessAdmin(adminAccessOptions.Value))
        {
            return NotFound();
        }

        var usage = await adminUsageService.GetUsageAsync(cancellationToken);
        return Ok(usage);
    }
}
