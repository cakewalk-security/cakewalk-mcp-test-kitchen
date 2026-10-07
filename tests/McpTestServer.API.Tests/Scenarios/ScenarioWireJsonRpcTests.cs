using System.Text.Json;
using FluentAssertions;
using McpTestServer.API.Scenarios;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

public sealed class ScenarioWireJsonRpcTests
{
    [Fact]
    public void FormatId_escapes_string_ids_for_json_embedding()
    {
        var formatted = ScenarioWireJsonRpc.FormatId("quote\"test");
        JsonSerializer.Deserialize<string>(formatted).Should().Be("quote\"test");
        ScenarioWireJsonRpc.FormatId(42).Should().Be("42");
        ScenarioWireJsonRpc.FormatId(null).Should().Be("null");
    }
}
