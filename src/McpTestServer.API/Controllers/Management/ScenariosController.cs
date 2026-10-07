using System.Text.Json.Serialization;
using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpTestServer.API.Controllers.Management;

[Authorize(Policy = AuthenticatedAccessConstants.PolicyName)]
[ApiController]
[Route("api/management/scenarios")]
public sealed class ScenariosController(IScenarioCatalog scenarioCatalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<ScenarioMetadataResponse>> List()
    {
        var scenarios = scenarioCatalog.ListMetadata()
            .Select(ScenarioMetadataResponse.FromMetadata)
            .ToList();
        return Ok(scenarios);
    }
}

public sealed record ScenarioMetadataResponse(
    string Id,
    string Title,
    string Description,
    string Area,
    string ParamsExampleJson,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ArticleUrl = null)
{
    public static ScenarioMetadataResponse FromMetadata(ScenarioMetadata metadata) =>
        new(metadata.Id, metadata.Title, metadata.Description, metadata.Area, metadata.ParamsExampleJson, metadata.ArticleUrl);
}
