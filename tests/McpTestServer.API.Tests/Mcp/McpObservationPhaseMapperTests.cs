using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Mcp;
using Xunit;

namespace McpTestServer.API.Tests.Mcp;

public sealed class McpObservationPhaseMapperTests
{
    [Fact]
    public void Map_initialize_returns_initialize_phase()
    {
        McpObservationPhaseMapper.Map(McpMethods.Initialize).Should().Be(ObservationPhases.Initialize);
    }

    [Fact]
    public void Map_server_discover_returns_server_discover_phase()
    {
        McpObservationPhaseMapper.Map(McpMethods.ServerDiscover).Should().Be(ObservationPhases.ServerDiscover);
    }

    [Fact]
    public void Map_notifications_initialized_returns_client_initialized_phase()
    {
        McpObservationPhaseMapper.Map(McpMethods.NotificationsInitialized)
            .Should().Be(ObservationPhases.ClientInitialized);
    }

    [Fact]
    public void Map_notification_prefix_returns_notification_phase()
    {
        McpObservationPhaseMapper.Map("notifications/cancelled")
            .Should().Be(ObservationPhases.Notification);
    }

    [Fact]
    public void Map_discovery_methods_return_discovery_phase()
    {
        McpObservationPhaseMapper.Map(McpMethods.ToolsList).Should().Be(ObservationPhases.Discovery);
        McpObservationPhaseMapper.Map(McpMethods.ResourcesList).Should().Be(ObservationPhases.Discovery);
    }

    [Fact]
    public void Map_tool_call_returns_tool_execution_phase()
    {
        McpObservationPhaseMapper.Map(McpMethods.ToolsCall).Should().Be(ObservationPhases.ToolExecution);
    }

    [Fact]
    public void Map_resource_and_prompt_fetch_methods_return_dedicated_phases()
    {
        McpObservationPhaseMapper.Map(McpMethods.ResourcesRead).Should().Be(ObservationPhases.ResourceRead);
        McpObservationPhaseMapper.Map(McpMethods.PromptsGet).Should().Be(ObservationPhases.PromptGet);
    }

    [Fact]
    public void Map_unknown_method_returns_protocol_phase()
    {
        McpObservationPhaseMapper.Map("ping").Should().Be(ObservationPhases.Protocol);
    }
}
