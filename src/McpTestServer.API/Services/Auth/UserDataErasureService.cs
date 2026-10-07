using McpTestServer.API.Scenarios;
using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace McpTestServer.API.Services.Auth;

public sealed class UserDataErasureService(
    McpTestServerContext db,
    IScenarioSessionRegistry sessionRegistry,
    IScenarioSessionFactory sessionFactory) : IUserDataErasureService
{
    public async Task EraseAllAsync(string email, CancellationToken cancellationToken)
    {
        sessionRegistry.TerminateSessionsForCaller(email);
        sessionFactory.ReleaseForCaller(email);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Observations
            .Where(o => o.CallerEmail != null && o.CallerEmail.ToLower() == email.ToLower())
            .ExecuteDeleteAsync(cancellationToken);

        await db.UserScenarioSelections
            .Where(s => s.Email.ToLower() == email.ToLower())
            .ExecuteDeleteAsync(cancellationToken);

        await db.UserMcpPats
            .Where(p => p.Email.ToLower() == email.ToLower())
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
