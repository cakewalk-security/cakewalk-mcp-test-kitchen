namespace McpTestServer.API.Scenarios;

public sealed record ScenarioMetadata(
    string Id,
    string Title,
    string Description,
    string Area,
    string ParamsExampleJson,
    string? ArticleUrl = null);
