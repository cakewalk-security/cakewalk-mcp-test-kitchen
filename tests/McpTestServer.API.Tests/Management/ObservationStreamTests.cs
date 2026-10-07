using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ObservationStreamTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ObservationStreamTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Stream_does_not_deliver_another_callers_observations()
    {
        using var aliceClient = _fixture.CreateAuthenticatedClient("stream-alice@example.com");
        using var bobClient = _fixture.CreateAuthenticatedClient("stream-bob@example.com");

        using var streamRequest = new HttpRequestMessage(HttpMethod.Get, "/api/management/observations/stream");
        streamRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        var streamTask = aliceClient.SendAsync(streamRequest, HttpCompletionOption.ResponseHeadersRead);

        var patB = await bobClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");
        using var mcpClientB = _fixture.CreateClient();
        await new McpSessionClient(mcpClientB, patB!.Pat).PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        var patA = await aliceClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");
        using var mcpClientA = _fixture.CreateClient();
        await new McpSessionClient(mcpClientA, patA!.Pat).PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var streamResponse = await streamTask.WaitAsync(TimeSpan.FromSeconds(10));
        streamResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var body = await streamResponse.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(body);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var sawAlice = false;
        while (!timeout.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(timeout.Token);
            if (line is null)
            {
                break;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line[5..].Trim();
            payload.Should().NotContain("stream-bob@example.com");
            if (payload.Contains("stream-alice@example.com", StringComparison.OrdinalIgnoreCase))
            {
                sawAlice = true;
                break;
            }
        }

        sawAlice.Should().BeTrue("Alice's live stream should receive her own observation");
    }
}
