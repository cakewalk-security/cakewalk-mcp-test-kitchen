using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Extensions;
using McpTestServer.API.Options;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Services.Auth;
using McpTestServer.API.Services.Scenarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Controllers.Management;

[Authorize(Policy = AuthenticatedAccessConstants.PolicyName)]
[ApiController]
[Route("api/management/me")]
public sealed class CurrentUserController(
    IUserMcpPatService userMcpPatService,
    IUserScenarioSelectionService selectionService,
    IUserDataErasureService userDataErasureService,
    IScenarioSessionRegistry sessionRegistry,
    IScenarioSessionFactory sessionFactory,
    IGoogleAuthAvailability googleAuthAvailability,
    IGitHubAuthAvailability githubAuthAvailability,
    IOptions<AdminAccessOptions> adminAccessOptions) : ControllerBase
{
    [HttpGet]
    public ActionResult<CurrentUserResponse> Get()
    {
        return Ok(new CurrentUserResponse(
            User.GetUserEmail(),
            googleAuthAvailability.IsConfigured,
            githubAuthAvailability.IsConfigured,
            McpProtocolConstants.LatestSupportedProtocolVersion,
            McpTransportConstants.Stateless,
            User.CanAccessAdmin(adminAccessOptions.Value)));
    }

    [HttpGet("scenario")]
    public async Task<ActionResult<UserScenarioSelectionResponse>> GetScenario(CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        var selection = await selectionService.GetOrDefaultAsync(email, cancellationToken);
        return Ok(new UserScenarioSelectionResponse(
            selection.ScenarioId,
            selection.ParamsJson,
            selection.UpdatedAt,
            UserScenarioSelectionResponse.AppliesToNextSessionMessage));
    }

    [HttpPut("scenario")]
    public async Task<ActionResult<UserScenarioSelectionResponse>> SetScenario(
        [FromBody] SetUserScenarioSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        try
        {
            var selection = await selectionService.SetAsync(
                email,
                request.ScenarioId,
                request.ParamsJson,
                cancellationToken);
            return Ok(new UserScenarioSelectionResponse(
                selection.ScenarioId,
                selection.ParamsJson,
                selection.UpdatedAt,
                UserScenarioSelectionResponse.AppliesToNextSessionMessage));
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpGet("sessions")]
    public ActionResult<IReadOnlyList<ScenarioSessionResponse>> ListSessions()
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        var sessions = sessionFactory.ListForCaller(email)
            .Select(session => new ScenarioSessionResponse(
                session.McpSessionId,
                session.ScenarioId,
                session.ParamsJson,
                session.StartedAt,
                session.GetInvocationCountsSnapshot()))
            .ToList();
        return Ok(sessions);
    }

    [HttpPost("sessions/terminate")]
    public IActionResult TerminateSessions()
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        sessionRegistry.TerminateSessionsForCaller(email);
        sessionFactory.ReleaseForCaller(email);
        return NoContent();
    }

    [HttpGet("pat")]
    [EnableRateLimiting(RateLimiterPolicyNames.PatEndpoints)]
    public async Task<ActionResult<UserMcpPatResponse>> GetPat(CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        var pat = await userMcpPatService.GetOrCreatePatAsync(email, cancellationToken);
        return Ok(new UserMcpPatResponse(email, pat));
    }

    [HttpPost("pat/regenerate")]
    [EnableRateLimiting(RateLimiterPolicyNames.PatEndpoints)]
    public async Task<ActionResult<UserMcpPatResponse>> RegeneratePat(CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        var pat = await userMcpPatService.RegeneratePatAsync(email, cancellationToken);
        return Ok(new UserMcpPatResponse(email, pat));
    }

    [HttpPost("erase")]
    [EnableRateLimiting(RateLimiterPolicyNames.EraseEndpoint)]
    public async Task<IActionResult> Erase(
        [FromBody] EraseUserDataRequest request,
        CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        if (!string.Equals(
                request.Confirmation?.Trim(),
                UserDataErasureConstants.ConfirmationPhrase,
                StringComparison.Ordinal))
        {
            return BadRequest();
        }

        await userDataErasureService.EraseAllAsync(email, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
