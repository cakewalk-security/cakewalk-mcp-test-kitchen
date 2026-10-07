using System.Security.Cryptography;
using McpTestServer.API.Constants;
using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpTestServer.API.Services.Auth;

public sealed class UserMcpPatService(
    McpTestServerContext db,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<UserMcpPatService> logger) : IUserMcpPatService
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector(UserMcpPatConstants.DataProtectionPurpose);

    public async Task<string> GetOrCreatePatAsync(string email, CancellationToken cancellationToken)
    {
        var existing = await db.UserMcpPats
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

        if (existing is not null)
        {
            try
            {
                return _protector.Unprotect(existing.ProtectedPat);
            }
            catch (CryptographicException)
            {
                logger.LogWarning(
                    "Stored MCP PAT could not be decrypted (UserMcpPatId={UserMcpPatId}). Regenerating.",
                    existing.Id);

                db.UserMcpPats.Remove(existing);
                await db.SaveChangesAsync(cancellationToken);
                return await CreatePatAsync(email, cancellationToken);
            }
        }

        return await CreatePatAsync(email, cancellationToken);
    }

    public async Task<string> RegeneratePatAsync(string email, CancellationToken cancellationToken)
    {
        var existing = await db.UserMcpPats
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

        if (existing is not null)
        {
            db.UserMcpPats.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await CreatePatAsync(email, cancellationToken);
    }

    private async Task<string> CreatePatAsync(string email, CancellationToken cancellationToken)
    {
        var pat = GeneratePat();
        var now = DateTime.UtcNow;

        db.UserMcpPats.Add(new UserMcpPat
        {
            Id = Guid.NewGuid(),
            Email = email,
            PatHash = UserMcpPatHasher.Hash(pat),
            ProtectedPat = _protector.Protect(pat),
            CreatedAt = now,
            UpdatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        return pat;
    }

    private static string GeneratePat()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"{UserMcpPatConstants.PatPrefix}{token}";
    }
}
