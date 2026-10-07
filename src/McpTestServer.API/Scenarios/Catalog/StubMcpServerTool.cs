using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class StubMcpServerTool : McpServerTool
{
    private readonly Tool _protocolTool;

    public StubMcpServerTool(string name, string description)
    {
        _protocolTool = new Tool
        {
            Name = name,
            Description = description,
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        };
    }

    public override Tool ProtocolTool => _protocolTool;

    public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            tool = _protocolTool.Name,
            message = "Stub tool executed.",
        });

        return ValueTask.FromResult(new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = payload,
                },
            ],
        });
    }
}
