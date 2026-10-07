using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace McpTestServer.API.Scenarios;

internal static class ScenarioToolResults
{
    public static CallToolResult Success(
        ScenarioSession session,
        int invocation,
        int? delayMs = null,
        string? message = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["invocation"] = invocation,
        };

        if (delayMs is not null)
        {
            payload["delayMs"] = delayMs;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            payload["message"] = message;
        }

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(payload),
                },
            ],
        };
    }

    public static CallToolResult ToolError(
        ScenarioSession session,
        int invocation,
        string errorMessage,
        string? message = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["invocation"] = invocation,
            ["errorMessage"] = errorMessage,
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            payload["message"] = message;
        }

        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(payload),
                },
            ],
        };
    }

    public static CallToolResult ElicitationOutcome(
        ScenarioSession session,
        int invocation,
        string outcome,
        string? reason,
        string? message = null,
        bool isError = false)
    {
        var payload = new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["invocation"] = invocation,
            ["elicitationOutcome"] = outcome,
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            payload["approvalReason"] = reason;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            payload["message"] = message;
        }

        return new CallToolResult
        {
            IsError = isError,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(payload),
                },
            ],
        };
    }
}
