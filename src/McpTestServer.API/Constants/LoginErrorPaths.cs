namespace McpTestServer.API.Constants;

public static class LoginErrorPaths
{
    public const string Page = "/login-error";

    public static string ForCode(string errorCode) =>
        $"{Page}?code={Uri.EscapeDataString(errorCode)}";
}
