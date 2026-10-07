import { describe, expect, it } from "vitest";
import {
  clampMessagesPaneWidth,
  computeMessagesPaneMaxWidth,
  MESSAGES_PANE_DEFAULT_WIDTH_PX,
  MESSAGES_PANE_MAX_WIDTH_PX,
  MESSAGES_PANE_MIN_WIDTH_PX,
  parseStoredMessagesPaneWidth,
  RESIZE_HANDLE_WIDTH_PX,
  SCENARIOS_PANE_MIN_WIDTH_PX,
} from "./workspaceSplit";

describe("clampMessagesPaneWidth", () => {
  it("clamps to the inclusive min and max", () => {
    expect(clampMessagesPaneWidth(100, 320, 720)).toBe(320);
    expect(clampMessagesPaneWidth(900, 320, 720)).toBe(720);
    expect(clampMessagesPaneWidth(480, 320, 720)).toBe(480);
  });
});

describe("parseStoredMessagesPaneWidth", () => {
  it("returns the default for missing or invalid values", () => {
    expect(parseStoredMessagesPaneWidth(null)).toBe(MESSAGES_PANE_DEFAULT_WIDTH_PX);
    expect(parseStoredMessagesPaneWidth("nope")).toBe(MESSAGES_PANE_DEFAULT_WIDTH_PX);
  });

  it("clamps a stored number to the pane range", () => {
    expect(parseStoredMessagesPaneWidth("480")).toBe(480);
    expect(parseStoredMessagesPaneWidth("100")).toBe(MESSAGES_PANE_MIN_WIDTH_PX);
    expect(parseStoredMessagesPaneWidth("9999")).toBe(MESSAGES_PANE_MAX_WIDTH_PX);
  });
});

describe("computeMessagesPaneMaxWidth", () => {
  it("lets the messages pane grow until the scenarios pane hits its minimum", () => {
    expect(computeMessagesPaneMaxWidth(1200)).toBe(
      1200 - SCENARIOS_PANE_MIN_WIDTH_PX - RESIZE_HANDLE_WIDTH_PX,
    );
    expect(computeMessagesPaneMaxWidth(800)).toBe(
      800 - SCENARIOS_PANE_MIN_WIDTH_PX - RESIZE_HANDLE_WIDTH_PX,
    );
  });

  it("still caps at the stored maximum on very wide screens", () => {
    expect(computeMessagesPaneMaxWidth(2200)).toBe(MESSAGES_PANE_MAX_WIDTH_PX);
  });
});
