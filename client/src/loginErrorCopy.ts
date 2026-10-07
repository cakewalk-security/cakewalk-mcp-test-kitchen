export const LOGIN_ERROR_CODES = {
  googleNotConfigured: "google_not_configured",
  githubNotConfigured: "github_not_configured",
  googleAuthFailed: "google_auth_failed",
  githubAuthFailed: "github_auth_failed",
} as const;

export type LoginErrorCopy = {
  title: string;
  detail: string;
};

const GENERIC_LOGIN_ERROR: LoginErrorCopy = {
  title: "Sign-in didn’t complete",
  detail: "Something went wrong while signing you in. Try again, or go back and pick a different login method.",
};

const LOGIN_ERROR_COPY: Record<string, LoginErrorCopy> = {
  [LOGIN_ERROR_CODES.googleNotConfigured]: {
    title: "Google login isn’t available",
    detail: "Google sign-in isn’t available right now. Try another login method, or come back later.",
  },
  [LOGIN_ERROR_CODES.githubNotConfigured]: {
    title: "GitHub login isn’t available",
    detail: "GitHub sign-in isn’t available right now. Try another login method, or come back later.",
  },
  [LOGIN_ERROR_CODES.googleAuthFailed]: {
    title: "Google sign-in failed",
    detail: "Google couldn’t finish signing you in. Try again, or use GitHub if that’s available.",
  },
  [LOGIN_ERROR_CODES.githubAuthFailed]: {
    title: "GitHub sign-in failed",
    detail: "GitHub couldn’t finish signing you in. Try again, or use Google if that’s available.",
  },
};

export function resolveLoginErrorCopy(code: string | null): LoginErrorCopy {
  if (!code) {
    return GENERIC_LOGIN_ERROR;
  }

  return LOGIN_ERROR_COPY[code] ?? GENERIC_LOGIN_ERROR;
}
