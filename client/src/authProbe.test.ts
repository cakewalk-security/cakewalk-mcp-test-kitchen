import { afterEach, describe, expect, it, vi } from "vitest";
import { AUTH_STATUS_PATH } from "./apiPaths";
import { isAuthenticatedUser } from "./authProbe";

describe("isAuthenticatedUser", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns true when auth status responds with authenticated true", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ authenticated: true }), {
          status: 200,
        }),
      ),
    );

    await expect(isAuthenticatedUser()).resolves.toBe(true);
    expect(fetch).toHaveBeenCalledWith(AUTH_STATUS_PATH, {
      credentials: "same-origin",
      headers: {
        Accept: "application/json",
      },
    });
  });

  it("returns false when auth status responds with authenticated false", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ authenticated: false }), {
          status: 200,
        }),
      ),
    );

    await expect(isAuthenticatedUser()).resolves.toBe(false);
  });

  it("returns false when auth status responds with a non-200 status", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 503 })));

    await expect(isAuthenticatedUser()).resolves.toBe(false);
  });

  it("does not redirect on unauthenticated status", async () => {
    const locationAssign = vi.fn();
    vi.stubGlobal("location", { href: "", assign: locationAssign });
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ authenticated: false }), {
          status: 200,
        }),
      ),
    );

    await isAuthenticatedUser();

    expect(locationAssign).not.toHaveBeenCalled();
  });
});
