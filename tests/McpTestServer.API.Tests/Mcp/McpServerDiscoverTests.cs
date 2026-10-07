using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Mcp;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class McpServerDiscoverTests
{
    private readonly McpTestServerApiFixture _fixture;

    public McpServerDiscoverTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Server_discover_advertises_the_native_stateless_protocol_and_is_logged_as_its_own_phase()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("server-discover@example.com");
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var mcpClient = _fixture.CreateClient();
        mcpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat!.Pat);

        var discoverJson =
            $$"""
            {
              "jsonrpc": "2.0",
              "id": 1,
              "method": "{{McpMethods.ServerDiscover}}",
              "params": {
                "_meta": {
                  "io.modelcontextprotocol/protocolVersion": "{{McpProtocolConstants.LatestSupportedProtocolVersion}}",
                  "io.modelcontextprotocol/clientCapabilities": {}
                }
              }
            }
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, McpPaths.McpEndpoint)
        {
            Content = new StringContent(discoverJson, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation(McpProtocolHeaders.ProtocolVersion, McpSessionClient.ProtocolVersion);
        request.Headers.TryAddWithoutValidation(McpProtocolHeaders.Method, McpMethods.ServerDiscover);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await mcpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "discover response: {0}", body);

        using var document = JsonDocument.Parse(ExtractJsonRpcPayload(body));
        var versions = document.RootElement.GetProperty("result").GetProperty("supportedVersions");
        versions.ValueKind.Should().Be(JsonValueKind.Array);
        versions.EnumerateArray().Select(element => element.GetString()).Should().Equal(
            McpProtocolConstants.LatestSupportedProtocolVersion);

        var observations = await managementClient.GetFromJsonAsync<ObservationListResponse>(
            "/api/management/observations");
        observations!.Items.Should().Contain(o =>
            o.Method == McpMethods.ServerDiscover && o.Phase == ObservationPhases.ServerDiscover);
    }

    private static string ExtractJsonRpcPayload(string body)
    {
        var dataLine = body.Split('\n').FirstOrDefault(line => line.StartsWith("data:", StringComparison.Ordinal));
        return dataLine is null ? body : dataLine[5..].Trim();
    }
}
