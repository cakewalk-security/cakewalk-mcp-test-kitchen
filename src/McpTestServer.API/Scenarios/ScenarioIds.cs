namespace McpTestServer.API.Scenarios;

public static class ScenarioIds
{
    public const string Baseline = "baseline";

    public const string TimeoutUncooperativeHang = "timeout.uncooperative_hang";

    public const string TimeoutCooperativeDeadline = "timeout.cooperative_deadline";

    public const string TimeoutLateResponse = "timeout.late_response";

    public const string TimeoutInitHang = "timeout.init_hang";

    public const string ErrorsHttpStatusSequence = "errors.http_status_sequence";

    public const string ErrorsJsonRpcError = "errors.jsonrpc_error";

    public const string ErrorsToolError = "errors.tool_error";

    public const string ErrorsConnectionClose = "errors.connection_close";

    public const string ErrorsMalformedPayload = "errors.malformed_payload";

    public const string ErrorsHttpStatusPerMethod = "errors.http_status_per_method";

    public const string ErrorsNonCompliantEnvelope = "errors.non_compliant_envelope";

    public const string AuthTokenLifecycle = "auth.token_lifecycle";

    public const string CatalogMutation = "catalog.mutation";

    public const string CatalogPagination = "catalog.pagination";

    public const string CatalogFacetUnavailable = "catalog.facet_unavailable";

    public const string ElicitationApproval = "elicitation.approval";

    public const string ElicitationDecline = "elicitation.decline";

    public const string ElicitationUrlMode = "elicitation.url_mode";

    public const string CompatSdkV2 = "compat.sdk_v2";
}
