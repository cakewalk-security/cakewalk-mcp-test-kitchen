using McpTestServer.API.Constants;
using McpTestServer.API.Options;
using McpTestServer.API.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Controllers.Account;

public sealed class AccountController(
    IGoogleAuthAvailability googleAuthAvailability,
    IGitHubAuthAvailability githubAuthAvailability) : Controller
{
    [HttpGet("/Account/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return Redirect(BuildLoginPageUrl(returnUrl));
    }

    [HttpGet("/Account/Login/Google")]
    public IActionResult LoginGoogle(string returnUrl = "/")
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        if (!googleAuthAvailability.IsConfigured)
        {
            return Redirect(LoginErrorPaths.ForCode(GoogleAuthErrors.GoogleNotConfigured));
        }

        return Challenge(CreateAccountSelectionChallengeProperties(returnUrl), AuthSchemes.OidcGoogle);
    }

    [HttpGet("/Account/Login/GitHub")]
    public IActionResult LoginGitHub(string returnUrl = "/")
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        if (!githubAuthAvailability.IsConfigured)
        {
            return Redirect(LoginErrorPaths.ForCode(GitHubAuthErrors.GitHubNotConfigured));
        }

        return Challenge(CreateAccountSelectionChallengeProperties(returnUrl), AuthSchemes.OAuthGitHub);
    }

    [HttpPost("/Account/Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect(LoginPaths.Page);
    }

    private string BuildLoginPageUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return LoginPaths.Page;
        }

        return $"{LoginPaths.Page}?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private static AuthenticationProperties CreateAccountSelectionChallengeProperties(string returnUrl)
    {
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        properties.SetParameter("prompt", OAuthAccountSelection.GooglePrompt);
        return properties;
    }

    private string NormalizeReturnUrl(string returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
    }
}
