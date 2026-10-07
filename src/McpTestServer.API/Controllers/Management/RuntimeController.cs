using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Extensions;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Services.Scenarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpTestServer.API.Controllers.Management;

[Authorize(Policy = AuthenticatedAccessConstants.PolicyName)]
[ApiController]
[Route("api/management/runtime")]
public sealed class RuntimeController(
    IScenarioSessionFactory sessionFactory,
    IUserScenarioSelectionService selectionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RuntimeStateResponse>> Get(CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        var sessions = sessionFactory.ListForCaller(email);
        if (sessions.Count == 0)
        {
            var selection = await selectionService.GetOrDefaultAsync(email, cancellationToken);
            return Ok(new RuntimeStateResponse(
                selection.ScenarioId,
                selection.ParamsJson,
                []));
        }

        var firstSession = sessions[0];
        return Ok(new RuntimeStateResponse(
            firstSession.ScenarioId,
            firstSession.ParamsJson,
            sessions
                .Select(session => new ScenarioSessionResponse(
                    session.McpSessionId,
                    session.ScenarioId,
                    session.ParamsJson,
                    session.StartedAt,
                    session.GetInvocationCountsSnapshot()))
                .ToList()));
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        foreach (var session in sessionFactory.ListForCaller(email))
        {
            session.ResetInvocationCounts();
        }

        return NoContent();
    }
}
