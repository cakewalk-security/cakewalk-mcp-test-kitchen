using System.Security.Cryptography;
using System.Text;
using McpTestServer.API.Options;
using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Services.Auth;

public sealed class UserMcpPatValidator(
    McpTestServerContext db,
    IOptions<McpPatOptions> options) : IUserMcpPatValidator
{
    public async Task<McpPatValidationResult> ValidateAsync(string providedToken, CancellationToken cancellationToken)
    {
        var patHash = UserMcpPatHasher.Hash(providedToken);
        var userPat = await db.UserMcpPats
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatHash == patHash, cancellationToken);

        if (userPat is not null)
        {
            return new McpPatValidationResult(true, userPat.Email);
        }

        var expectedToken = options.Value.MCP_PAT;
        if (!string.IsNullOrEmpty(expectedToken))
        {
            var expected = Encoding.UTF8.GetBytes(expectedToken);
            var provided = Encoding.UTF8.GetBytes(providedToken);

            if (expected.Length == provided.Length &&
                CryptographicOperations.FixedTimeEquals(expected, provided))
            {
                return new McpPatValidationResult(true, null);
            }
        }

        return new McpPatValidationResult(false, null);
    }
}
