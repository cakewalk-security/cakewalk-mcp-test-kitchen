import type { ScenarioMetadata } from "./types";

export type ScenarioParamField = {
  key: string;
  type: string;
  description: string;
};

const AREA_LABELS: Record<string, string> = {
  baseline: "Baseline",
  timeout: "Timeouts",
  errors: "HTTP & protocol errors",
  auth: "Auth",
  catalog: "Catalog",
  elicitation: "Elicitation",
  compat: "C# SDK v2",
};

const PARAM_FIELDS: Record<string, ScenarioParamField[]> = {
  baseline: [
    {
      key: "slowReportDelayMs",
      type: "number",
      description: "Milliseconds run_configured_test_scenario waits before returning. Use 0 for an immediate response.",
    },
  ],
  "errors.http_status_sequence": [
    {
      key: "statusCodes",
      type: "number[]",
      description:
        "HTTP status codes returned in order, one per MCP POST request. Codes in the 2xx range let the request continue to the MCP handler.",
    },
  ],
  "errors.jsonrpc_error": [
    {
      key: "code",
      type: "number",
      description: "JSON-RPC error code returned on the configured tools/call invocation.",
    },
    {
      key: "message",
      type: "string",
      description: "JSON-RPC error message.",
    },
    {
      key: "onInvocation",
      type: "number",
      description: "Which tools/call invocation returns the JSON-RPC error.",
    },
  ],
  "errors.tool_error": [
    {
      key: "errorMessage",
      type: "string",
      description: "Message included in the isError tool result.",
    },
    {
      key: "onInvocation",
      type: "number",
      description: "Which run_configured_test_scenario invocation returns isError=true.",
    },
  ],
  "elicitation.approval": [
    {
      key: "promptMessage",
      type: "string",
      description: "Message shown when the server asks the client to elicit user approval.",
    },
    {
      key: "reasonFieldTitle",
      type: "string",
      description: "Label for the required approval reason field in the elicitation form.",
    },
    {
      key: "reasonMaxLength",
      type: "number",
      description: "Maximum length of the approval reason.",
    },
    {
      key: "requireReason",
      type: "boolean",
      description: "Whether the approval reason is required before the scenario continues.",
    },
    {
      key: "elicitationTimeoutMs",
      type: "number",
      description: "How long the server waits for the client to complete elicitation.",
    },
  ],
  "compat.sdk_v2": [
    {
      key: "defaultCloseReason",
      type: "string",
      description: "Default close reason proposed during MRTR elicitation and used when the client accepts without a value.",
    },
    {
      key: "promptMessage",
      type: "string",
      description: "Elicitation prompt for simulate_ticket_close_mrtr. Use {ticketId} as a placeholder.",
    },
  ],
};

export function getScenarioAreaLabel(area: string): string {
  return AREA_LABELS[area] ?? area;
}

export function getScenarioParamFields(scenarioId: string): ScenarioParamField[] {
  return PARAM_FIELDS[scenarioId] ?? [];
}

export function formatParamsJson(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value) as unknown, null, 2);
  } catch {
    return value;
  }
}

export function findScenario(scenarios: ScenarioMetadata[], scenarioId: string): ScenarioMetadata | undefined {
  return scenarios.find((scenario) => scenario.id === scenarioId);
}

export function paramsMatchExample(paramsJson: string, exampleJson: string): boolean {
  try {
    const params = JSON.parse(paramsJson) as unknown;
    const example = JSON.parse(exampleJson) as unknown;
    return JSON.stringify(params) === JSON.stringify(example);
  } catch {
    return false;
  }
}

export function isParamsDirty(paramsJson: string, exampleJson: string): boolean {
  const trimmed = paramsJson.trim();
  if (!trimmed || trimmed === "{}") {
    return false;
  }

  return !paramsMatchExample(paramsJson, exampleJson);
}
