namespace McpTestServer.API.Services.Database;

public interface IDatabaseReadiness
{
    bool IsReady { get; }

    void MarkReady();
}
