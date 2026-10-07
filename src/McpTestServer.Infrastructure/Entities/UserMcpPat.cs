namespace McpTestServer.Infrastructure.Entities;

public sealed class UserMcpPat
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PatHash { get; set; } = string.Empty;

    public string ProtectedPat { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
