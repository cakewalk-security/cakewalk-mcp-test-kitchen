using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.Infrastructure.Constants;

namespace McpTestServer.API.Tests.Fixtures;

public sealed class McpSessionClient
{
    public const string ProtocolVersion = "2026-07-28";

    public const string LegacyProtocolVersion = "2025-11-25";

    private const string ProtocolVersionHeader = "MCP-Protocol-Version";
    private const string EventStreamMediaType = "text/event-stream";
    private const string SseDataPrefix = "data:";

    private readonly HttpClient _client;

    public McpSessionClient(HttpClient client, string pat)
    {
        _client = client;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(EventStreamMediaType));
    }

    /// <summary>
    /// Posts an MCP JSON-RPC message without a session header.
    /// Defaults to legacy protocol for broad stateless back-compat; pass <see cref="ProtocolVersion"/> for native 2026-07-28.
    /// </summary>
    public Task<HttpResponseMessage> PostStatelessAsync(object body, string? protocolVersion = null) =>
        PostStatelessAsync(_client, body, protocolVersion ?? LegacyProtocolVersion);

    public static async Task<HttpResponseMessage> PostStatelessAsync(
        HttpClient client,
        object body,
        string? protocolVersion = null)
    {
        using var request = BuildRequest(sessionId: null, JsonContent.Create(body), protocolVersion ?? LegacyProtocolVersion);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Legacy initialize handshake for 2025-11-25 back-compat tests. May return an empty session id in stateless mode.
    /// </summary>
    public async Task<string> InitializeSessionAsync(object? capabilities = null, string? protocolVersion = null)
    {
        var version = protocolVersion ?? LegacyProtocolVersion;
        var initBody = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.Initialize,
            @params = new
            {
                protocolVersion = version,
                capabilities = capabilities ?? new { },
                clientInfo = new { name = "mcp-test-server-tests", version = "1.0.0" },
            },
        };

        using var initResponse = await PostMcpAsync(_client, sessionId: null, initBody, version);
        initResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var sessionId = initResponse.Headers.TryGetValues(AuthHeaderNames.McpSessionId, out var sessionHeaders)
            ? sessionHeaders.FirstOrDefault()
            : null;

        var notifiedBody = new
        {
            jsonrpc = "2.0",
            method = McpMethods.NotificationsInitialized,
            @params = new { },
        };
        using var notifiedResponse = await PostMcpAsync(_client, sessionId, notifiedBody, version);
        notifiedResponse.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.Accepted);

        return sessionId ?? string.Empty;
    }

    public Task<HttpResponseMessage> PostMcpAsync(string? sessionId, object body) =>
        PostMcpAsync(_client, sessionId, body);

    public static Task<HttpResponseMessage> PostMcpAsync(HttpClient client, string? sessionId, object body) =>
        PostMcpAsync(client, sessionId, body, ProtocolVersion);

    public static async Task<HttpResponseMessage> PostMcpAsync(
        HttpClient client,
        string? sessionId,
        object body,
        string protocolVersion)
    {
        using var request = BuildRequest(sessionId, JsonContent.Create(body), protocolVersion);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Posts a message and returns as soon as response headers arrive, so the caller can read
    /// server-to-client requests off the SSE stream while the originating call is still in flight.
    /// </summary>
    public async Task<HttpResponseMessage> PostMcpStreamingAsync(
        string? sessionId,
        object body,
        CancellationToken cancellationToken)
    {
        using var request = BuildRequest(sessionId, JsonContent.Create(body), ProtocolVersion);
        return await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostMcpJsonAsync(
        string? sessionId,
        string json,
        CancellationToken cancellationToken)
    {
        using var request = BuildRequest(sessionId, new StringContent(json, Encoding.UTF8, "application/json"), ProtocolVersion);
        return await _client.SendAsync(request, cancellationToken);
    }

    public static async IAsyncEnumerable<JsonDocument> ReadSseEventsAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith(SseDataPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            yield return JsonDocument.Parse(line[SseDataPrefix.Length..].Trim());
        }
    }

    public static async Task<JsonDocument> ReadMcpJsonAsync(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (mediaType.StartsWith(EventStreamMediaType, StringComparison.OrdinalIgnoreCase))
        {
            var raw = await response.Content.ReadAsStringAsync();
            var dataLine = raw.Split('\n').FirstOrDefault(l => l.StartsWith(SseDataPrefix, StringComparison.Ordinal));
            dataLine.Should().NotBeNull();
            return JsonDocument.Parse(dataLine![SseDataPrefix.Length..].Trim());
        }

        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    private static HttpRequestMessage BuildRequest(string? sessionId, HttpContent content, string protocolVersion)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, McpPaths.McpEndpoint)
        {
            Content = content,
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            request.Headers.TryAddWithoutValidation(AuthHeaderNames.McpSessionId, sessionId);
        }

        request.Headers.TryAddWithoutValidation(ProtocolVersionHeader, protocolVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(EventStreamMediaType));
        return request;
    }
}
