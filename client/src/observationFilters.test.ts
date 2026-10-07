import { describe, expect, it } from "vitest";
import {
  buildObservationListQuery,
  EMPTY_OBSERVATION_FILTERS,
  observationBelongsToViewer,
  observationMatchesFilters,
  observationPrimaryLabel,
  OBSERVATIONS_DEFAULT_PAGE,
  OBSERVATIONS_PAGE_SIZE,
} from "./observationFilters";

describe("buildObservationListQuery", () => {
  it("includes page and limit", () => {
    const query = buildObservationListQuery(OBSERVATIONS_DEFAULT_PAGE, OBSERVATIONS_PAGE_SIZE, EMPTY_OBSERVATION_FILTERS);
    expect(query).toContain("page=1");
    expect(query).toContain("limit=50");
  });

  it("includes active filters", () => {
    const query = buildObservationListQuery(2, 25, {
      ...EMPTY_OBSERVATION_FILTERS,
      method: "tools/call",
      phase: "tool_execution",
      caller: "alice",
      fromDate: "2026-01-01",
      toDate: "2026-01-31",
    });

    expect(query).toContain("page=2");
    expect(query).toContain("limit=25");
    expect(query).toContain("method=tools%2Fcall");
    expect(query).toContain("phase=tool_execution");
    expect(query).toContain("caller=alice");
    const from = new Date(2026, 0, 1, 0, 0, 0, 0).toISOString();
    const to = new Date(2026, 0, 31, 23, 59, 59, 999).toISOString();
    expect(query).toContain(`from=${encodeURIComponent(from)}`);
    expect(query).toContain(`to=${encodeURIComponent(to)}`);
  });

  it("uses the local calendar day rather than UTC midnight", () => {
    const fromIso = new Date(2026, 0, 1, 0, 0, 0, 0).toISOString();
    const utcMidnight = "2026-01-01T00:00:00.000Z";
    if (new Date(2026, 0, 1).getTimezoneOffset() !== 0) {
      expect(fromIso).not.toBe(utcMidnight);
    }

    const query = buildObservationListQuery(1, 25, {
      ...EMPTY_OBSERVATION_FILTERS,
      fromDate: "2026-01-01",
    });
    expect(query).toContain(`from=${encodeURIComponent(fromIso)}`);
  });

  it("includes category when not all", () => {
    const query = buildObservationListQuery(1, 25, {
      ...EMPTY_OBSERVATION_FILTERS,
      category: "resources",
    });
    expect(query).toContain("category=resources");
  });
});

describe("observationMatchesFilters", () => {
  const baseObservation = {
    occurredAt: "2026-01-15T12:00:00.000Z",
    method: "tools/call",
    phase: "tool_execution",
    callerEmail: "alice@example.com",
  };

  it("matches when filters are empty", () => {
    expect(observationMatchesFilters(baseObservation, EMPTY_OBSERVATION_FILTERS)).toBe(true);
  });

  it("filters by method, phase, and caller fragment", () => {
    expect(
      observationMatchesFilters(baseObservation, {
        ...EMPTY_OBSERVATION_FILTERS,
        method: "tools/call",
        phase: "tool_execution",
        caller: "alice",
      }),
    ).toBe(true);

    expect(
      observationMatchesFilters(baseObservation, {
        ...EMPTY_OBSERVATION_FILTERS,
        method: "initialize",
      }),
    ).toBe(false);
  });

  it("filters by category", () => {
    expect(
      observationMatchesFilters(baseObservation, {
        ...EMPTY_OBSERVATION_FILTERS,
        category: "tools",
      }),
    ).toBe(true);

    expect(
      observationMatchesFilters(baseObservation, {
        ...EMPTY_OBSERVATION_FILTERS,
        category: "resources",
      }),
    ).toBe(false);
  });
});

describe("observationBelongsToViewer", () => {
  it("keeps the viewer's own rows and drops everyone else", () => {
    expect(
      observationBelongsToViewer({ callerEmail: "alice@example.com" }, "alice@example.com"),
    ).toBe(true);
    expect(
      observationBelongsToViewer({ callerEmail: "Alice@Example.com" }, "alice@example.com"),
    ).toBe(true);
    expect(
      observationBelongsToViewer({ callerEmail: "bob@example.com" }, "alice@example.com"),
    ).toBe(false);
  });

  it("drops unattributed rows and rows seen before the viewer email is known", () => {
    expect(observationBelongsToViewer({ callerEmail: undefined }, "alice@example.com")).toBe(false);
    expect(observationBelongsToViewer({ callerEmail: "alice@example.com" }, undefined)).toBe(false);
    expect(observationBelongsToViewer({ callerEmail: "alice@example.com" }, "  ")).toBe(false);
  });
});

describe("observationPrimaryLabel", () => {
  it("prefers scenario id for tool traffic", () => {
    expect(
      observationPrimaryLabel({
        method: "tools/call",
        scenarioId: "baseline",
      }),
    ).toBe("baseline");
  });

  it("shows resource uri when scenario is absent", () => {
    expect(
      observationPrimaryLabel({
        method: "resources/read",
        requestParamsJson: "{\"uri\":\"test://resources/valid\"}",
      }),
    ).toBe("test://resources/valid");
  });
});
