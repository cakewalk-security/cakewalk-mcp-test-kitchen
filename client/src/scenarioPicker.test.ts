import { describe, expect, it } from "vitest";
import { isValidScenarioParamsJson } from "./scenarioPicker";

describe("isValidScenarioParamsJson", () => {
  it("accepts object JSON", () => {
    expect(isValidScenarioParamsJson('{"slowReportDelayMs":0}')).toBe(true);
  });

  it("rejects arrays, primitives, and invalid JSON", () => {
    expect(isValidScenarioParamsJson("[]")).toBe(false);
    expect(isValidScenarioParamsJson('"text"')).toBe(false);
    expect(isValidScenarioParamsJson("not-json")).toBe(false);
    expect(isValidScenarioParamsJson("")).toBe(false);
  });
});
