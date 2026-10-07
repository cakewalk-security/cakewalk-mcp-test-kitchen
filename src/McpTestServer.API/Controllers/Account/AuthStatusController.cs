using Microsoft.AspNetCore.Mvc;

namespace McpTestServer.API.Controllers.Account;

[ApiController]
[Route("api/auth")]
public sealed class AuthStatusController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<AuthStatusResponse> GetStatus()
    {
        return Ok(new AuthStatusResponse(User.Identity?.IsAuthenticated == true));
    }
}

public sealed record AuthStatusResponse(bool Authenticated);
