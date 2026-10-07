import { afterEach, describe, expect, it, vi } from "vitest";
import { runObservationStreamLoop } from "./observationStream";

describe("runObservationStreamLoop", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns a stop function synchronously so effect cleanup can abort immediately", () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() => new Promise<Response>(() => undefined)),
    );

    const onDisconnected = vi.fn();
    const stop = runObservationStreamLoop({
      onConnected: vi.fn(),
      onDisconnected,
      onObservation: vi.fn(),
    });

    expect(typeof stop).toBe("function");
    stop();
    expect(onDisconnected).toHaveBeenCalled();
  });
});
