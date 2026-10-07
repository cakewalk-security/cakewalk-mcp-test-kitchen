using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace McpTestServer.API.Services.Admin;

public sealed class AdminUsageService(
    McpTestServerContext db,
    IScenarioSessionFactory sessionFactory) : IAdminUsageService
{
    public async Task<AdminUsageResponse> GetUsageAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var since24Hours = now.AddHours(-24);
        var since7Days = now.AddDays(-7);

        var registeredUsers = await db.UserMcpPats
            .AsNoTracking()
            .Select(pat => new RegisteredUser(pat.Email, pat.CreatedAt))
            .ToListAsync(cancellationToken);

        var liveSessions = sessionFactory.ListAll()
            .Where(session => !IsAnonymousCaller(session.CallerEmail))
            .ToList();

        var observationStats = await db.Observations
            .AsNoTracking()
            .Where(observation => observation.CallerEmail != null)
            .GroupBy(observation => observation.CallerEmail!)
            .Select(group => new ObservationUserStats(
                group.Key,
                group.Max(observation => observation.OccurredAt),
                group.Count(observation => observation.OccurredAt >= since24Hours),
                group.Count(observation => observation.OccurredAt >= since7Days),
                group.OrderByDescending(observation => observation.OccurredAt)
                    .Select(observation => observation.ScenarioId)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var registeredByEmail = registeredUsers.ToDictionary(
            user => user.Email,
            user => user,
            StringComparer.OrdinalIgnoreCase);

        var observationByEmail = observationStats.ToDictionary(
            stats => stats.Email,
            stats => stats,
            StringComparer.OrdinalIgnoreCase);

        var liveSessionsByEmail = liveSessions
            .GroupBy(session => session.CallerEmail!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        var allEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var email in registeredByEmail.Keys)
        {
            allEmails.Add(email);
        }

        foreach (var email in observationByEmail.Keys)
        {
            allEmails.Add(email);
        }

        var users = allEmails
            .Select(email =>
            {
                registeredByEmail.TryGetValue(email, out var registered);
                observationByEmail.TryGetValue(email, out var observations);
                liveSessionsByEmail.TryGetValue(email, out var liveSessionCount);

                return new AdminUsageUserRow(
                    EmailMasking.MaskEmail(email),
                    registered?.RegisteredAt,
                    observations?.LastSeenAt,
                    observations?.CountLast24Hours ?? 0,
                    observations?.CountLast7Days ?? 0,
                    liveSessionCount,
                    observations?.LastScenarioId);
            })
            .OrderByDescending(user => user.LastSeenAt ?? DateTime.MinValue)
            .ThenBy(user => user.MaskedEmail, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var observationsLast24Hours = await db.Observations
            .AsNoTracking()
            .CountAsync(observation => observation.OccurredAt >= since24Hours, cancellationToken);

        var observationsLast7Days = await db.Observations
            .AsNoTracking()
            .CountAsync(observation => observation.OccurredAt >= since7Days, cancellationToken);

        var activeUsersLast24Hours = observationStats.Count(stats => stats.CountLast24Hours > 0);

        var totals = new AdminUsageTotalsResponse(
            registeredUsers.Count,
            liveSessionsByEmail.Count,
            liveSessions.Count,
            activeUsersLast24Hours,
            observationsLast24Hours,
            observationsLast7Days);

        return new AdminUsageResponse(totals, users);
    }

    private static bool IsAnonymousCaller(string? callerEmail) =>
        string.IsNullOrWhiteSpace(callerEmail)
        || string.Equals(callerEmail, ScenarioSessionCallerKeys.AnonymousCallerKey, StringComparison.Ordinal);

    private sealed record RegisteredUser(string Email, DateTime RegisteredAt);

    private sealed record ObservationUserStats(
        string Email,
        DateTime LastSeenAt,
        int CountLast24Hours,
        int CountLast7Days,
        string? LastScenarioId);
}
