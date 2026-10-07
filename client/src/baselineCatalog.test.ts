import { describe, expect, it } from "vitest";
import {
  BASELINE_PROMPTS,
  BASELINE_RESOURCES,
  getCatalogFacetHint,
  getCatalogFacetTitle,
  matchesBaselinePromptQuery,
  matchesBaselineResourceQuery,
} from "./baselineCatalog";

describe("baselineCatalog", () => {
  it("documents the four always-on catalog items", () => {
    expect(BASELINE_RESOURCES.map((item) => item.name)).toEqual(["valid_resource", "invalid_resource"]);
    expect(BASELINE_PROMPTS.map((item) => item.name)).toEqual(["valid_prompt", "invalid_prompt"]);
  });

  it("filters resources and prompts by search query", () => {
    expect(matchesBaselineResourceQuery(BASELINE_RESOURCES[0], "valid")).toBe(true);
    expect(matchesBaselineResourceQuery(BASELINE_RESOURCES[0], "invalid")).toBe(false);
    expect(matchesBaselinePromptQuery(BASELINE_PROMPTS[1], "invalid")).toBe(true);
  });

  it("uses scenario wording only for the tools facet", () => {
    expect(getCatalogFacetTitle("tools")).toBe("Scenarios");
    expect(getCatalogFacetTitle("resources")).toBe("Resources");
    expect(getCatalogFacetHint("resources", true)).toContain("no scenario configuration");
  });
});
