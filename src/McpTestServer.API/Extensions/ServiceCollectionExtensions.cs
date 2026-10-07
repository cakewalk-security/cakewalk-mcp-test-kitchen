using System.Security.Claims;
using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Baseline;
using McpTestServer.API.Scenarios.Compat;
using McpTestServer.API.Scenarios.Elicitation;
using McpTestServer.API.Scenarios.Errors;
using McpTestServer.API.Options;
using McpTestServer.API.Options.Validation;
using McpTestServer.API.Services.Admin;
using McpTestServer.API.Services.Auth;
using McpTestServer.API.Services.Database;
using McpTestServer.API.Services.Feedback;
using McpTestServer.API.Services.Observations;
using McpTestServer.API.Services.Scenarios;
using McpTestServer.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Server;
using Resend;
using System.Threading.RateLimiting;

namespace McpTestServer.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(options =>
        {
            configuration.GetSection(DatabaseOptions.SectionName).Bind(options);
            var databaseUrl = configuration[DatabaseConnectionStringResolver.DatabaseUrlEnv];
            options.CONNECTION_STRING = DatabaseConnectionStringResolver.Resolve(
                databaseUrl,
                options.CONNECTION_STRING);
        });
        services.Configure<GoogleCredentialsOptions>(configuration.Bind);
        services.Configure<GitHubCredentialsOptions>(configuration.Bind);
        services.Configure<AdminAccessOptions>(configuration.Bind);
        services.Configure<McpPatOptions>(configuration.GetSection(McpPatOptions.SectionName));
        services.Configure<ObservationRetentionOptions>(options =>
        {
            if (int.TryParse(configuration[ObservationRetentionOptions.RetentionHoursEnv], out var retentionHours))
            {
                options.RetentionHours = retentionHours;
            }

            if (int.TryParse(configuration[ObservationRetentionOptions.MaxRowsEnv], out var maxRows))
            {
                options.MaxRows = maxRows;
            }
        });
        services.Configure<FeedbackOptions>(options =>
        {
            var recipient = configuration[FeedbackOptions.FeedbackRecipientEnv];
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                options.Recipient = recipient;
            }

            var fromAddress = configuration[FeedbackOptions.FeedbackFromEnv];
            if (!string.IsNullOrWhiteSpace(fromAddress))
            {
                options.FromAddress = fromAddress;
            }
        });

        services.AddOptions<ResendClientOptions>()
            .Configure(options =>
            {
                options.ApiToken = configuration[FeedbackOptions.ResendApiTokenEnv] ?? string.Empty;
            });
        services.AddHttpClient<IResend, ResendClient>();

        services.AddOptions<OAuthPublicOriginOptions>()
            .Bind(configuration)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OAuthPublicOriginOptions>, OAuthPublicOriginOptionsValidator>();

        services.AddOptions<UnauthenticatedRedirectOptions>()
            .Bind(configuration)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<UnauthenticatedRedirectOptions>, UnauthenticatedRedirectOptionsValidator>();

        services.AddOptions<LegacyHostRedirectOptions>()
            .Bind(configuration)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<LegacyHostRedirectOptions>, LegacyHostRedirectOptionsValidator>();

        services.AddOptions<McpPatOptions>()
            .Bind(configuration.GetSection(McpPatOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<McpPatOptions>, McpPatOptionsValidator>();

        services.AddMcpTestServerDatabase();
        services.AddSingleton<IDatabaseReadiness, DatabaseReadiness>();
        services.AddHostedService<DatabaseMigrationHostedService>();

        services.AddHttpContextAccessor();
        services.AddSingleton<IScenarioSessionRegistry, ScenarioSessionRegistry>();
        services.AddSingleton<IScenarioSessionFactory, ScenarioSessionFactory>();
        services.AddSingleton<IObservationStreamHub, ObservationStreamHub>();
        services.AddScoped<IUserScenarioSelectionService, UserScenarioSelectionService>();
        services.AddScoped<IObservationRecorder, ObservationRecorder>();
        services.AddScoped<IUserMcpPatService, UserMcpPatService>();
        services.AddScoped<IUserMcpPatValidator, UserMcpPatValidator>();
        services.AddScoped<IUserDataErasureService, UserDataErasureService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IAdminUsageService, AdminUsageService>();
        services.AddSingleton<IGoogleAuthAvailability, GoogleAuthAvailability>();
        services.AddSingleton<IGitHubAuthAvailability, GitHubAuthAvailability>();
        services.AddHostedService<ObservationRetentionService>();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthenticatedAccessConstants.PolicyName, policy =>
                policy.RequireAuthenticatedUser());

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimiterPolicyNames.PatEndpoints, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirstValue(ClaimTypes.Email) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimiterPartitionKeys.Unknown,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimiterLimits.PatEndpointsPermitLimit,
                        Window = TimeSpan.FromMinutes(RateLimiterLimits.PatEndpointsWindowMinutes),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimiterPolicyNames.FeedbackEndpoint, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirstValue(ClaimTypes.Email) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimiterPartitionKeys.Unknown,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimiterLimits.FeedbackEndpointPermitLimit,
                        Window = TimeSpan.FromMinutes(RateLimiterLimits.FeedbackEndpointWindowMinutes),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimiterPolicyNames.EraseEndpoint, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirstValue(ClaimTypes.Email) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimiterPartitionKeys.Unknown,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimiterLimits.EraseEndpointPermitLimit,
                        Window = TimeSpan.FromMinutes(RateLimiterLimits.EraseEndpointWindowMinutes),
                        QueueLimit = 0,
                    }));
        });

        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(McpTestServerDataProtectionConstants.ApplicationName);

        var dataProtectionKeysPath = configuration[McpTestServerHostEnv.DataProtectionKeysPath];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
        }

        services.AddControllers();
        services.AddOpenApi();

        var googleCredentials = configuration.Get<GoogleCredentialsOptions>() ?? new GoogleCredentialsOptions();
        var hasGoogleAuth = !string.IsNullOrWhiteSpace(googleCredentials.INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID);
        var githubCredentials = configuration.Get<GitHubCredentialsOptions>() ?? new GitHubCredentialsOptions();
        var hasGitHubAuth = !string.IsNullOrWhiteSpace(githubCredentials.INTERNAL_AUTHENTICATION_GITHUB_CLIENT_ID);

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureCookieSecurePolicy>();

        if (hasGoogleAuth)
        {
            authenticationBuilder.AddOpenIdConnect(AuthSchemes.OidcGoogle, "Google", options =>
            {
                options.CallbackPath = OAuthCallbackPaths.GoogleSignIn;
                options.Authority = "https://accounts.google.com";
                options.ClientId = googleCredentials.INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID;
                options.ClientSecret = googleCredentials.INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_SECRET;
                options.Prompt = OAuthAccountSelection.GooglePrompt;
                options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ValidateIssuer = true,
                    ValidIssuers = GoogleOidcClaims.ValidIssuers,
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.Prompt = OAuthAccountSelection.GooglePrompt;

                        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var publicOrigin = configuration[OAuthPublicOriginOptions.EnvKey];
                        if (!string.IsNullOrWhiteSpace(publicOrigin))
                        {
                            context.ProtocolMessage.RedirectUri =
                                $"{publicOrigin.TrimEnd('/')}{OAuthCallbackPaths.GoogleSignIn}";
                            return Task.CompletedTask;
                        }

                        var env = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                        if (!env.IsDevelopment())
                        {
                            context.ProtocolMessage.RedirectUri =
                                $"https://{context.Request.Host}{OAuthCallbackPaths.GoogleSignIn}";
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var email = context.Principal?.GetUserEmail();
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            context.Fail("Email claim is required.");
                            return Task.CompletedTask;
                        }

                        if (!context.Principal!.HasVerifiedGoogleEmail())
                        {
                            context.Fail("A verified email is required.");
                            return Task.CompletedTask;
                        }

                        AuthTicketExtensions.AddAuthProviderClaim(context.Principal!, AuthProviderClaims.Google);
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                        RedirectToLoginError(context, GoogleAuthErrors.GoogleAuthFailed),
                };
            });
        }

        if (hasGitHubAuth)
        {
            authenticationBuilder.AddGitHub(AuthSchemes.OAuthGitHub, options =>
            {
                options.ClientId = githubCredentials.INTERNAL_AUTHENTICATION_GITHUB_CLIENT_ID;
                options.ClientSecret = githubCredentials.INTERNAL_AUTHENTICATION_GITHUB_CLIENT_SECRET;
                options.CallbackPath = OAuthCallbackPaths.GitHubSignIn;
                options.Scope.Add("user:email");

                options.Events = new OAuthEvents
                {
                    OnRedirectToAuthorizationEndpoint = context =>
                        RedirectToOAuthProviderWithPublicCallback(context, OAuthCallbackPaths.GitHubSignIn),
                    OnCreatingTicket = context =>
                    {
                        var email = context.Principal?.GetUserEmail();
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            context.Fail("Email claim is required.");
                            return Task.CompletedTask;
                        }

                        AuthTicketExtensions.AddAuthProviderClaim(context.Principal!, AuthProviderClaims.GitHub);
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                        RedirectToLoginError(context, GitHubAuthErrors.GitHubAuthFailed),
                };
            });
        }

        // Enabled scenarios. Other implementations under Scenarios/ stay in the repo but are not
        // registered; add an AddScenario<T>() line here to expose one in the admin UI again.
        services.AddScenario<BaselineScenario>();
        services.AddScenario<HttpStatusSequenceScenario>();
        services.AddScenario<JsonRpcErrorScenario>();
        services.AddScenario<ToolErrorScenario>();
        services.AddScenario<ElicitationApprovalScenario>();
        services.AddScenario<SdkV2CompatScenario>();
        services.AddScenarioCatalog();

        services.AddAuthorization();

        services.AddMcpServer()
            .WithHttpTransport(httpOptions =>
            {
                httpOptions.Stateless = McpTransportConstants.Stateless;
                httpOptions.ConfigureSessionOptions = ScenarioSessionConfigurator.ConfigureSessionOptionsAsync;
            });

        return services;
    }

    private static Task RedirectToLoginError(RemoteFailureContext context, string errorCode)
    {
        context.Response.Redirect(LoginErrorPaths.ForCode(errorCode));
        context.HandleResponse();
        return Task.CompletedTask;
    }

    private static Task RedirectToOAuthProviderWithPublicCallback(
        RedirectContext<OAuthOptions> context,
        string callbackPath)
    {
        var callbackUri = ResolveOAuthCallbackRedirectUri(context.HttpContext, callbackPath);
        if (!string.IsNullOrWhiteSpace(callbackUri))
        {
            context.RedirectUri = ReplaceOAuthCallbackRedirectUri(context.RedirectUri, callbackUri);
        }

        context.Response.Redirect(OAuthAccountSelection.WrapGitHubAuthorizeUrl(context.RedirectUri));
        return Task.CompletedTask;
    }

    private static string ReplaceOAuthCallbackRedirectUri(string authorizationRedirectUri, string callbackUri)
    {
        var uri = new Uri(authorizationRedirectUri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var updated = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query)
        {
            updated[pair.Key] = pair.Value.ToString();
        }

        updated["redirect_uri"] = callbackUri;
        return QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path), updated);
    }

    private static string? ResolveOAuthCallbackRedirectUri(HttpContext httpContext, string callbackPath)
    {
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var publicOrigin = configuration[OAuthPublicOriginOptions.EnvKey];
        if (!string.IsNullOrWhiteSpace(publicOrigin))
        {
            return $"{publicOrigin.TrimEnd('/')}{callbackPath}";
        }

        var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
        {
            return $"https://{httpContext.Request.Host}{callbackPath}";
        }

        return null;
    }

    private sealed class ConfigureCookieSecurePolicy(IHostEnvironment environment) : IConfigureOptions<CookieAuthenticationOptions>
    {
        public void Configure(CookieAuthenticationOptions options)
        {
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        }
    }
}
