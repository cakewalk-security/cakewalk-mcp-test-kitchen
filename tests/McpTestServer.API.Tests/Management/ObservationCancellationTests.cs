using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ObservationCancellationTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ObservationCancellationTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Terminate_cancels_an_in_flight_tools_call()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("terminate-inflight-user@example.com");
        await ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":8000}""");

        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { } },
        };

        var callTask = mcp.PostStatelessAsync(callBody);
        await Task.Delay(300);

        using var terminateResponse = await managementClient.PostAsync(
            "/api/management/me/sessions/terminate",
            content: null);
        terminateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var finished = await Task.WhenAny(callTask, Task.Delay(TimeSpan.FromSeconds(2)));
        finished.Should().Be(callTask);

        try
        {
            using var response = await callTask;
        }
        catch (Exception)
        {
            // Aborting the in-flight request may surface as a client transport error.
        }

        var payload = await WaitForCancelledObservationAsync(managementClient);
        payload.Items.Should().Contain(o => o.WasCancelled);
    }

    [Fact]
    public async Task Oversized_chunked_mcp_body_returns_jsonrpc_parse_error_not_500()
    {
        // A declared Content-Length over the cap is rejected with 413 up front (McpRequestBodySizeTests).
        // A chunked body carries no length, so it reaches the buffer limit instead. Kestrel would cap it
        // too, but TestServer does not apply Kestrel limits, which lets this exercise the buffer path.
        using var client = _fixture.CreateClient();
        var oversized = new string('a', McpObservationConstants.RequestBodyBufferLimitBytes + 1);
        var bytes = Encoding.UTF8.GetBytes($"{{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"{oversized}\"}}");
        using var content = new StreamContent(new MemoryStream(bytes));
        content.Headers.ContentType = new MediaTypeHeaderValue(HttpMediaTypes.ApplicationJson);
        using var request = new HttpRequestMessage(HttpMethod.Post, McpPaths.McpEndpoint) { Content = content };
        request.Headers.TransferEncodingChunked = true;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.McpPat);

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpMediaTypes.ApplicationJson);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(McpJsonRpcErrorCodes.ParseError.ToString());
    }

    private static async Task<ObservationListResponse> WaitForCancelledObservationAsync(HttpClient managementClient)
    {
        ObservationListResponse? payload = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            payload = await managementClient.GetFromJsonAsync<ObservationListResponse>(
                "/api/management/observations?limit=20");
            if (payload?.Items.Any(o => o.WasCancelled) == true)
            {
                return payload;
            }

            await Task.Delay(50);
        }

        payload.Should().NotBeNull();
        return payload!;
    }
}
