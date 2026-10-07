import { describe, expect, it } from "vitest";
import {
  ACCOUNT_LOGIN_GITHUB_PATH,
  ACCOUNT_LOGIN_GOOGLE_PATH,
  ADMIN_PATH,
  buildAccountLoginUrl,
  buildGitHubLoginUrl,
  buildGoogleLoginUrl,
  CONSOLE_PATH,
  LOGIN_ERROR_PATH,
  LOGIN_PATH,
  resolveAppRoute,
  resolveReturnUrl,
} from "./appRoutes";

describe("resolveAppRoute", () => {
  it("returns console for /console", () => {
    expect(resolveAppRoute(CONSOLE_PATH)).toBe("console");
  });

  it("returns admin for /admin", () => {
    expect(resolveAppRoute(ADMIN_PATH)).toBe("admin");
  });

  it("returns login for /login", () => {
    expect(resolveAppRoute(LOGIN_PATH)).toBe("login");
  });

  it("returns login-error for /login-error", () => {
    expect(resolveAppRoute(LOGIN_ERROR_PATH)).toBe("login-error");
  });

  it("returns redirect for /", () => {
    expect(resolveAppRoute("/")).toBe("redirect");
  });

  it("returns redirect for unknown paths", () => {
    expect(resolveAppRoute("/unknown")).toBe("redirect");
    expect(resolveAppRoute("/api/management/me")).toBe("redirect");
  });
});

describe("resolveReturnUrl", () => {
  it("returns fallback for missing or unsafe values", () => {
    expect(resolveReturnUrl(null)).toBe(CONSOLE_PATH);
    expect(resolveReturnUrl("")).toBe(CONSOLE_PATH);
    expect(resolveReturnUrl("//evil.example")).toBe(CONSOLE_PATH);
    expect(resolveReturnUrl("https://evil.example")).toBe(CONSOLE_PATH);
    expect(resolveReturnUrl("/\\evil.example")).toBe(CONSOLE_PATH);
    expect(resolveReturnUrl("/\t/evil.example")).toBe(CONSOLE_PATH);
  });

  it("returns local paths unchanged", () => {
    expect(resolveReturnUrl("/admin")).toBe("/admin");
  });
});

describe("buildGoogleLoginUrl", () => {
  it("builds Google login URL with encoded return path", () => {
    expect(buildGoogleLoginUrl(CONSOLE_PATH)).toBe(
      `${ACCOUNT_LOGIN_GOOGLE_PATH}?returnUrl=%2Fconsole`,
    );
  });
});

describe("buildGitHubLoginUrl", () => {
  it("builds GitHub login URL with encoded return path", () => {
    expect(buildGitHubLoginUrl(CONSOLE_PATH)).toBe(
      `${ACCOUNT_LOGIN_GITHUB_PATH}?returnUrl=%2Fconsole`,
    );
  });
});

describe("buildAccountLoginUrl", () => {
  it("builds login page URL without return path", () => {
    expect(buildAccountLoginUrl()).toBe(LOGIN_PATH);
  });

  it("builds login page URL with encoded return path", () => {
    expect(buildAccountLoginUrl(CONSOLE_PATH)).toBe("/login?returnUrl=%2Fconsole");
  });
});
