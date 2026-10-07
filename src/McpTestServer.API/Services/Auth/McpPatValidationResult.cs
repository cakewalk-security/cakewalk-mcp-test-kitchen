namespace McpTestServer.API.Services.Auth;

public sealed record McpPatValidationResult(bool IsValid, string? CallerEmail);
