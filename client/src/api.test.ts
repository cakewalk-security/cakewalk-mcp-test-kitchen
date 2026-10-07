import { afterEach, describe, expect, it, vi } from "vitest";
import { apiFetch } from "./api";

describe("apiFetch", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns undefined for empty 204 No Content responses", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(null, {
          status: 204,
        }),
      ),
    );

    await expect(apiFetch("/api/management/me/sessions/terminate", { method: "POST" })).resolves.toBeUndefined();
  });

  it("parses JSON response bodies", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ scenarioId: "baseline" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      ),
    );

    await expect(apiFetch<{ scenarioId: string }>("/api/management/me/scenario")).resolves.toEqual({
      scenarioId: "baseline",
    });
  });
});
