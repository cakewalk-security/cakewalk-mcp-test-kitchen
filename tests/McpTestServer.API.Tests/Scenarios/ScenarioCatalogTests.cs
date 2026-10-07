using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Baseline;
using McpTestServer.API.Scenarios.Errors;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

public sealed class ScenarioCatalogTests
{
    [Fact]
    public void Register_duplicate_scenario_id_throws()
    {
        var catalog = new ScenarioCatalog();
        catalog.Register(new BaselineScenario());

        var act = () => catalog.Register(new BaselineScenario());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetRequired_unknown_id_throws()
    {
        var catalog = new ScenarioCatalog();

        var act = () => catalog.GetRequired("missing");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ListMetadata_returns_registered_scenarios()
    {
        var catalog = new ScenarioCatalog();
        catalog.Register(new BaselineScenario());
        catalog.Register(new JsonRpcErrorScenario());

        catalog.ListMetadata().Select(m => m.Id).Should().Contain(
        [
            ScenarioIds.Baseline,
            ScenarioIds.ErrorsJsonRpcError,
        ]);
    }

    [Fact]
    public void BindOrDefault_uses_defaults_for_empty_json()
    {
        var parameters = ScenarioParamsBinder.BindOrDefault("{}", new BaselineParams());
        parameters.SlowReportDelayMs.Should().Be(0);
    }

    [Fact]
    public void BindOrDefault_deserializes_baseline_params()
    {
        var parameters = ScenarioParamsBinder.BindOrDefault("""{"slowReportDelayMs":250}""", new BaselineParams());
        parameters.SlowReportDelayMs.Should().Be(250);
    }
}

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ScenariosEndpointTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ScenariosEndpointTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task List_scenarios_returns_registered_metadata()
    {
        using var client = _fixture.CreateAuthenticatedClient();
        var scenarios = await client.GetFromJsonAsync<List<ScenarioMetadataResponse>>("/api/management/scenarios");
        scenarios.Should().NotBeNull();
        scenarios.Should().NotBeEmpty();
        var scenarioIds = scenarios!.Select(s => s.Id).ToList();
        scenarioIds.Should().OnlyHaveUniqueItems();
        scenarioIds.Should().BeEquivalentTo(
        [
            ScenarioIds.Baseline,
            ScenarioIds.ErrorsHttpStatusSequence,
            ScenarioIds.ErrorsJsonRpcError,
            ScenarioIds.ErrorsToolError,
            ScenarioIds.ElicitationApproval,
            ScenarioIds.CompatSdkV2,
        ]);
        var compat = scenarios!.Single(s => s.Id == ScenarioIds.CompatSdkV2);
        compat.Title.Should().Contain("C# SDK");
        compat.ArticleUrl.Should().Be(McpCsharpSdkV2Article.Url);
    }
}
