using Microsoft.AspNetCore.WebUtilities;

namespace McpTestServer.API.Services.Auth;

public static class OAuthAccountSelection
{
    public const string GooglePrompt = "select_account";

    public static string WrapGitHubAuthorizeUrl(string authorizeUrl) =>
        QueryHelpers.AddQueryString("https://github.com/login", "return_to", authorizeUrl);
}
