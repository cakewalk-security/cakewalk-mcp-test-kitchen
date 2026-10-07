import { describe, expect, it } from "vitest";
import {
  formatParamsJson,
  getScenarioAreaLabel,
  getScenarioParamFields,
  isParamsDirty,
  paramsMatchExample,
} from "./scenarioDisplay";

describe("formatParamsJson", () => {
  it("pretty-prints valid JSON", () => {
    expect(formatParamsJson('{"statusCodes":[503,200]}')).toBe(
      '{\n  "statusCodes": [\n    503,\n    200\n  ]\n}',
    );
  });
});

describe("paramsMatchExample", () => {
  it("matches equivalent JSON objects regardless of spacing", () => {
    expect(paramsMatchExample('{"slowReportDelayMs":0}', '{"slowReportDelayMs": 0}')).toBe(true);
  });
});

describe("isParamsDirty", () => {
  it("treats empty object as not dirty", () => {
    expect(isParamsDirty("{}", '{"slowReportDelayMs":0}')).toBe(false);
  });

  it("detects edits away from the example", () => {
    expect(isParamsDirty('{"slowReportDelayMs":500}', '{"slowReportDelayMs":0}')).toBe(true);
  });
});

describe("getScenarioParamFields", () => {
  it("returns documented fields for known scenarios", () => {
    expect(getScenarioParamFields("errors.http_status_sequence")).toHaveLength(1);
    expect(getScenarioParamFields("errors.http_status_sequence")[0]?.key).toBe("statusCodes");
    expect(getScenarioParamFields("errors.jsonrpc_error")).toHaveLength(3);
    expect(getScenarioParamFields("errors.tool_error")).toHaveLength(2);
    expect(getScenarioParamFields("elicitation.approval")).toHaveLength(5);
    expect(getScenarioParamFields("compat.sdk_v2")).toHaveLength(2);
    expect(getScenarioParamFields("compat.sdk_v2")[0]?.key).toBe("defaultCloseReason");
    expect(getScenarioAreaLabel("compat")).toBe("C# SDK v2");
  });

  it("returns an empty list for unknown scenarios", () => {
    expect(getScenarioParamFields("missing.scenario")).toEqual([]);
  });
});
