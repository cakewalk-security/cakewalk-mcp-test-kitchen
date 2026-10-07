import { describe, expect, it } from "vitest";
import { ERASE_EVERYTHING_CONFIRMATION_PHRASE, isEraseEverythingConfirmation } from "./eraseEverything";

describe("isEraseEverythingConfirmation", () => {
  it("accepts the exact confirmation phrase", () => {
    expect(isEraseEverythingConfirmation(ERASE_EVERYTHING_CONFIRMATION_PHRASE)).toBe(true);
  });

  it("accepts surrounding whitespace", () => {
    expect(isEraseEverythingConfirmation(`  ${ERASE_EVERYTHING_CONFIRMATION_PHRASE}  `)).toBe(true);
  });

  it("rejects a mismatched or empty phrase", () => {
    expect(isEraseEverythingConfirmation("")).toBe(false);
    expect(isEraseEverythingConfirmation("Erase Everything")).toBe(false);
    expect(isEraseEverythingConfirmation("erase")).toBe(false);
  });
});
