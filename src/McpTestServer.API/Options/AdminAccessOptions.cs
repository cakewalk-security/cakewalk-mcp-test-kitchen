namespace McpTestServer.API.Options;

public sealed class AdminAccessOptions
{
    public string MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN { get; set; } = string.Empty;

    public bool IsAdminEmail(string? email)
    {
        var domain = AdminDomain;
        return !string.IsNullOrWhiteSpace(domain)
            && !string.IsNullOrWhiteSpace(email)
            && email.EndsWith($"@{domain}", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAdminDomain(string? domain)
    {
        var adminDomain = AdminDomain;
        return !string.IsNullOrWhiteSpace(adminDomain)
            && string.Equals(domain?.Trim(), adminDomain, StringComparison.OrdinalIgnoreCase);
    }

    private string AdminDomain => MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN.Trim().TrimStart('@');
}
