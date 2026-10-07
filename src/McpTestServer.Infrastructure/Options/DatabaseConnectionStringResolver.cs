using Npgsql;

namespace McpTestServer.Infrastructure.Options;

public static class DatabaseConnectionStringResolver
{
    public const string DatabaseUrlEnv = "DATABASE_URL";

    public static string Resolve(string? databaseUrl, string? sectionConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return NormalizeConnectionString(databaseUrl);
        }

        if (!string.IsNullOrWhiteSpace(sectionConnectionString))
        {
            return sectionConnectionString;
        }

        return new DatabaseOptions().CONNECTION_STRING;
    }

    private static string NormalizeConnectionString(string databaseUrl)
    {
        var trimmed = databaseUrl.Trim();
        if (!IsPostgresUri(trimmed))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo;
        var colonIndex = userInfo.IndexOf(':');
        var username = colonIndex >= 0
            ? Uri.UnescapeDataString(userInfo[..colonIndex])
            : Uri.UnescapeDataString(userInfo);
        var password = colonIndex >= 0 && colonIndex < userInfo.Length - 1
            ? Uri.UnescapeDataString(userInfo[(colonIndex + 1)..])
            : string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = username,
            Password = password,
        };

        ApplyQueryParameters(builder, uri.Query);

        return builder.ConnectionString;
    }

    private static bool IsPostgresUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static void ApplyQueryParameters(NpgsqlConnectionStringBuilder builder, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var trimmedQuery = query.TrimStart('?');
        foreach (var segment in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(segment[..separatorIndex]);
            var value = Uri.UnescapeDataString(segment[(separatorIndex + 1)..]);

            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = value.ToLowerInvariant() switch
                    {
                        "disable" => SslMode.Disable,
                        "allow" => SslMode.Allow,
                        "prefer" => SslMode.Prefer,
                        "require" => SslMode.Require,
                        "verify-ca" or "verifyca" => SslMode.VerifyCA,
                        "verify-full" or "verifyfull" => SslMode.VerifyFull,
                        _ => builder.SslMode,
                    };
                    break;
            }
        }
    }
}
