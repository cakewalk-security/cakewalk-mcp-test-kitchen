namespace McpTestServer.API.Scenarios;

public static class ScenarioWireHelpers
{
    public static bool ShouldApplyOnInvocation(int invocation, int onInvocation) =>
        invocation == onInvocation;

    public static int IncrementHttpRequestInvocation(ScenarioSession session) =>
        session.IncrementInvocationCount(ScenarioWireInvocationKeys.HttpRequest);
}
