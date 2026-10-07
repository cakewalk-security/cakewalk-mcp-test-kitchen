namespace McpTestServer.API.Services.Database;

public sealed class DatabaseReadiness : IDatabaseReadiness
{
    public bool IsReady { get; private set; }

    public void MarkReady() => IsReady = true;
}
