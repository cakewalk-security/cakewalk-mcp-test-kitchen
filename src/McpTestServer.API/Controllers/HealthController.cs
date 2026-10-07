using McpTestServer.API.Services.Database;
using Microsoft.AspNetCore.Mvc;

namespace McpTestServer.API.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(IDatabaseReadiness databaseReadiness) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        if (!databaseReadiness.IsReady)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Ok();
    }
}
