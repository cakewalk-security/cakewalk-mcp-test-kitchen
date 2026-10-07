export const CONSOLE_PATH = "/console";

export const ADMIN_PATH = "/admin";

export const ACCOUNT_LOGIN_PATH = "/Account/Login";

export const ACCOUNT_LOGIN_GOOGLE_PATH = "/Account/Login/Google";

export const ACCOUNT_LOGIN_GITHUB_PATH = "/Account/Login/GitHub";

export const LOGIN_PATH = "/login";

export const LOGIN_ERROR_PATH = "/login-error";

export type AppRoute = "console" | "admin" | "login" | "login-error" | "redirect";

export function resolveAppRoute(pathname: string): AppRoute {
  if (pathname === CONSOLE_PATH) {
    return "console";
  }

  if (pathname === ADMIN_PATH) {
    return "admin";
  }

  if (pathname === LOGIN_PATH) {
    return "login";
  }

  if (pathname === LOGIN_ERROR_PATH) {
    return "login-error";
  }

  return "redirect";
}

export function resolveReturnUrl(returnUrl: string | null, fallback = CONSOLE_PATH): string {
  if (!isLocalPath(returnUrl)) {
    return fallback;
  }

  return returnUrl;
}

// Browsers treat "/\host" like "//host", and strip tabs/newlines before parsing,
// so prefix checks alone are not enough: resolve the URL and require the same origin.
function isLocalPath(value: string | null): value is string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) {
    return false;
  }

  try {
    const base = "http://local.invalid";
    return new URL(value, base).origin === base;
  } catch {
    return false;
  }
}

export function buildGoogleLoginUrl(returnUrl: string): string {
  return `${ACCOUNT_LOGIN_GOOGLE_PATH}?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export function buildGitHubLoginUrl(returnUrl: string): string {
  return `${ACCOUNT_LOGIN_GITHUB_PATH}?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export function buildAccountLoginUrl(returnUrl?: string): string {
  if (!returnUrl) {
    return LOGIN_PATH;
  }

  return `${LOGIN_PATH}?returnUrl=${encodeURIComponent(returnUrl)}`;
}
