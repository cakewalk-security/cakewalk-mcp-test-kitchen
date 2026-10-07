using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Extensions;
using McpTestServer.API.Options;
using McpTestServer.API.Services.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace McpTestServer.API.Controllers.Management;

[Authorize(Policy = AuthenticatedAccessConstants.PolicyName)]
[ApiController]
[Route("api/management/feedback")]
[EnableRateLimiting(RateLimiterPolicyNames.FeedbackEndpoint)]
public sealed class FeedbackController(IFeedbackService feedbackService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message)
            || request.Message.Trim().Length > FeedbackOptions.MaxMessageLength)
        {
            return BadRequest();
        }

        var email = User.GetUserEmail();
        await feedbackService.SendAsync(email, request.Message.Trim(), cancellationToken);
        return NoContent();
    }
}
