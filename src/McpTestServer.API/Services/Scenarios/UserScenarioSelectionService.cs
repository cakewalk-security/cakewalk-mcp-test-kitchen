using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Auth;
using McpTestServer.API.Scenarios.Baseline;
using McpTestServer.API.Scenarios.Catalog;
using McpTestServer.API.Scenarios.Compat;
using McpTestServer.API.Scenarios.Elicitation;
using McpTestServer.API.Scenarios.Errors;
using McpTestServer.API.Scenarios.Timeout;
using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpTestServer.API.Services.Scenarios;

public sealed record UserScenarioSelectionSnapshot(string ScenarioId, string ParamsJson, DateTime UpdatedAt);

public interface IUserScenarioSelectionService
{
    Task<UserScenarioSelectionSnapshot> GetOrDefaultAsync(string email, CancellationToken cancellationToken);

    Task<UserScenarioSelectionSnapshot> SetAsync(
        string email,
        string scenarioId,
        string paramsJson,
        CancellationToken cancellationToken);
}

public sealed class UserScenarioSelectionService(
    McpTestServerContext db,
    IScenarioCatalog scenarioCatalog) : IUserScenarioSelectionService
{
    public async Task<UserScenarioSelectionSnapshot> GetOrDefaultAsync(string email, CancellationToken cancellationToken)
    {
        var selection = await db.UserScenarioSelections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

        if (selection is null)
        {
            return new UserScenarioSelectionSnapshot(ScenarioIds.Baseline, "{}", DateTime.UtcNow);
        }

        return new UserScenarioSelectionSnapshot(selection.ScenarioId, selection.ParamsJson, selection.UpdatedAt);
    }

    public async Task<UserScenarioSelectionSnapshot> SetAsync(
        string email,
        string scenarioId,
        string paramsJson,
        CancellationToken cancellationToken)
    {
        if (!scenarioCatalog.TryGet(scenarioId, out _))
        {
            throw new ArgumentException($"Unknown scenario id '{scenarioId}'.", nameof(scenarioId));
        }

        ValidateParamsForScenario(scenarioId, paramsJson);

        var now = DateTime.UtcNow;
        var existing = await db.UserScenarioSelections.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
        if (existing is null)
        {
            existing = new UserScenarioSelection
            {
                Email = email,
                ScenarioId = scenarioId,
                ParamsJson = paramsJson,
                UpdatedAt = now,
            };
            db.UserScenarioSelections.Add(existing);
        }
        else
        {
            existing.ScenarioId = scenarioId;
            existing.ParamsJson = paramsJson;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new UserScenarioSelectionSnapshot(existing.ScenarioId, existing.ParamsJson, existing.UpdatedAt);
    }

    private void ValidateParamsForScenario(string scenarioId, string paramsJson)
    {
        switch (scenarioId)
        {
            case ScenarioIds.Baseline:
                scenarioCatalog.ValidateParams<BaselineParams>(paramsJson);
                break;
            case ScenarioIds.ErrorsHttpStatusSequence:
                scenarioCatalog.ValidateParams<HttpStatusSequenceParams>(paramsJson);
                break;
            case ScenarioIds.ErrorsJsonRpcError:
                scenarioCatalog.ValidateParams<JsonRpcErrorParams>(paramsJson);
                break;
            case ScenarioIds.ErrorsToolError:
                scenarioCatalog.ValidateParams<ToolErrorParams>(paramsJson);
                break;
            case ScenarioIds.ElicitationApproval:
                scenarioCatalog.ValidateParams<ElicitationApprovalParams>(paramsJson);
                break;
            case ScenarioIds.CompatSdkV2:
                scenarioCatalog.ValidateParams<SdkV2CompatParams>(paramsJson);
                break;
            case ScenarioIds.TimeoutUncooperativeHang:
                scenarioCatalog.ValidateParams<TimeoutUncooperativeHangParams>(paramsJson);
                break;
            case ScenarioIds.AuthTokenLifecycle:
                scenarioCatalog.ValidateParams<TokenLifecycleParams>(paramsJson);
                break;
            case ScenarioIds.CatalogMutation:
                scenarioCatalog.ValidateParams<CatalogMutationParams>(paramsJson);
                break;
            case ScenarioIds.CatalogFacetUnavailable:
                scenarioCatalog.ValidateParams<CatalogFacetUnavailableParams>(paramsJson);
                break;
            case ScenarioIds.ErrorsHttpStatusPerMethod:
                scenarioCatalog.ValidateParams<HttpStatusPerMethodParams>(paramsJson);
                break;
            default:
                if (!ScenarioParamsBinder.TryBind<Dictionary<string, object>>(paramsJson, out _, out var error))
                {
                    throw new ArgumentException(error ?? "Invalid params JSON.", nameof(paramsJson));
                }

                break;
        }
    }
}
