using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Elicitation;

public static class ElicitationMrtrConstants
{
    public const string InputResponseKey = "elicitation";

    public const string RequestStateAwaiting = "awaiting-elicitation";
}

internal static class ElicitationMrtrHelper
{
    public static bool TryGetElicitResult(
        RequestContext<CallToolRequestParams> request,
        out ElicitResult? elicit,
        string inputResponseKey = ElicitationMrtrConstants.InputResponseKey)
    {
        elicit = null;
        if (request.Params?.InputResponses?.TryGetValue(inputResponseKey, out var response) != true)
        {
            return false;
        }

        elicit = response!.Deserialize(InputResponse.ElicitResultJsonTypeInfo);
        return true;
    }

    [DoesNotReturn]
    public static void ThrowElicitation(
        ElicitRequestParams elicitParams,
        string requestState = ElicitationMrtrConstants.RequestStateAwaiting,
        string inputResponseKey = ElicitationMrtrConstants.InputResponseKey)
    {
        throw new InputRequiredException(
            inputRequests: new Dictionary<string, InputRequest>
            {
                [inputResponseKey] = InputRequest.ForElicitation(elicitParams),
            },
            requestState: requestState);
    }
}
