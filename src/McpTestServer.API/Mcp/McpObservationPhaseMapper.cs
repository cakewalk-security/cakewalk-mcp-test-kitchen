using McpTestServer.API.Constants;

namespace McpTestServer.API.Mcp;

public static class McpObservationPhaseMapper
{
    public static string Map(string method)
    {
        if (method == McpMethods.ServerDiscover)
        {
            return ObservationPhases.ServerDiscover;
        }

        if (method == McpMethods.Initialize)
        {
            return ObservationPhases.Initialize;
        }

        if (method == McpMethods.NotificationsInitialized)
        {
            return ObservationPhases.ClientInitialized;
        }

        if (method.StartsWith("notifications/", StringComparison.Ordinal))
        {
            return ObservationPhases.Notification;
        }

        if (method is McpMethods.ToolsList or McpMethods.ResourcesList or McpMethods.PromptsList or McpMethods.ToolsListChanged)
        {
            return ObservationPhases.Discovery;
        }

        if (method is McpMethods.ToolsCall)
        {
            return ObservationPhases.ToolExecution;
        }

        if (method == McpMethods.ResourcesRead)
        {
            return ObservationPhases.ResourceRead;
        }

        if (method == McpMethods.PromptsGet)
        {
            return ObservationPhases.PromptGet;
        }

        return ObservationPhases.Protocol;
    }
}
