import { describe, expect, it } from "vitest";
import { LOGIN_ERROR_CODES, resolveLoginErrorCopy } from "./loginErrorCopy";

describe("resolveLoginErrorCopy", () => {
  it("returns provider-specific copy for known codes", () => {
    expect(resolveLoginErrorCopy(LOGIN_ERROR_CODES.githubAuthFailed).title).toBe("GitHub sign-in failed");
    expect(resolveLoginErrorCopy(LOGIN_ERROR_CODES.googleNotConfigured).title).toBe(
      "Google login isn’t available",
    );
  });

  it("returns generic copy for missing or unknown codes", () => {
    expect(resolveLoginErrorCopy(null).title).toBe("Sign-in didn’t complete");
    expect(resolveLoginErrorCopy("nope").title).toBe("Sign-in didn’t complete");
  });
});
