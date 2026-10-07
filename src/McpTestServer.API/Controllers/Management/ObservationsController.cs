using System.Text.Json;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Extensions;
using McpTestServer.API.Mcp;
using McpTestServer.API.Services.Observations;
using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace McpTestServer.API.Controllers.Management;

[Authorize(Policy = AuthenticatedAccessConstants.PolicyName)]
[ApiController]
[Route("api/management/observations")]
public sealed class ObservationsController(
    McpTestServerContext db,
    IObservationStreamHub observationStreamHub) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<ActionResult<ObservationListResponse>> List(
        [FromQuery(Name = ObservationQueryParams.Page)] int page = ObservationQueryParams.DefaultPage,
        [FromQuery(Name = ObservationQueryParams.Limit)] int limit = ObservationQueryParams.DefaultLimit,
        [FromQuery(Name = ObservationQueryParams.From)] DateTime? from = null,
        [FromQuery(Name = ObservationQueryParams.To)] DateTime? to = null,
        [FromQuery(Name = ObservationQueryParams.Method)] string? method = null,
        [FromQuery(Name = ObservationQueryParams.Phase)] string? phase = null,
        [FromQuery(Name = ObservationQueryParams.Category)] string? category = null,
        [FromQuery(Name = ObservationQueryParams.Caller)] string? caller = null,
        CancellationToken cancellationToken = default)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        var cappedPage = Math.Max(page, 1);
        var cappedLimit = Math.Clamp(limit, 1, ObservationQueryParams.MaxLimit);

        var query = ScopeToCaller(db.Observations.AsNoTracking(), email);

        if (from.HasValue)
        {
            query = query.Where(o => o.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.OccurredAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(method))
        {
            query = query.Where(o => o.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(phase))
        {
            query = query.Where(o => o.Phase == phase);
        }

        if (!string.IsNullOrWhiteSpace(category)
            && !string.Equals(category, McpObservationCatalog.CategoryAll, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = normalizedCategory switch
            {
                McpObservationCatalog.CategoryTools =>
                    query.Where(o => o.Method.StartsWith("tools/")),
                McpObservationCatalog.CategoryResources =>
                    query.Where(o => o.Method.StartsWith("resources/")),
                McpObservationCatalog.CategoryPrompts =>
                    query.Where(o => o.Method.StartsWith("prompts/")),
                _ => query,
            };
        }

        if (!string.IsNullOrWhiteSpace(caller))
        {
            var callerPattern = $"%{caller.Trim()}%";
            query = query.Where(o => o.CallerEmail != null && EF.Functions.ILike(o.CallerEmail, callerPattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var observations = await query
            .OrderByDescending(o => o.OccurredAt)
            .Skip((cappedPage - 1) * cappedLimit)
            .Take(cappedLimit)
            .ToListAsync(cancellationToken);

        return Ok(new ObservationListResponse(
            observations.Select(ObservationResponse.FromEntity).ToList(),
            totalCount,
            cappedPage,
            cappedLimit));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest();
        }

        await ScopeToCaller(db.Observations, email).ExecuteDeleteAsync(cancellationToken);
        return NoContent();
    }

    private static IQueryable<Observation> ScopeToCaller(IQueryable<Observation> query, string email) =>
        query.Where(o => o.CallerEmail != null && o.CallerEmail.ToLower() == email.ToLower());

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        var email = User.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var bodyFeature = HttpContext.Features.Get<IHttpResponseBodyFeature>();
        if (bodyFeature is not null)
        {
            bodyFeature.DisableBuffering();
        }

        Response.Headers.ContentType = HttpMediaTypes.TextEventStream;
        Response.Headers.CacheControl = McpObservationConstants.SseCacheControl;
        Response.Headers.Append(
            McpObservationConstants.SseAccelBufferingHeader,
            McpObservationConstants.SseAccelBufferingDisabled);

        var reader = observationStreamHub.Subscribe(email, out var writer);
        var heartbeatInterval = TimeSpan.FromSeconds(McpObservationConstants.SseHeartbeatIntervalSeconds);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                waitCts.CancelAfter(heartbeatInterval);
                try
                {
                    if (!await reader.WaitToReadAsync(waitCts.Token))
                    {
                        break;
                    }

                    while (reader.TryRead(out var observation))
                    {
                        var json = JsonSerializer.Serialize(observation, JsonOptions);
                        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (waitCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    await Response.WriteAsync(McpObservationConstants.SseHeartbeatComment, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            observationStreamHub.Unsubscribe(writer);
        }
    }
}
