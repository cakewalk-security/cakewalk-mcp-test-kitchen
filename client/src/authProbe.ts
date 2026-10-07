import { AUTH_STATUS_PATH } from "./apiPaths";

type AuthStatusResponse = {
  authenticated: boolean;
};

export async function isAuthenticatedUser(): Promise<boolean> {
  const response = await fetch(AUTH_STATUS_PATH, {
    credentials: "same-origin",
    headers: {
      Accept: "application/json",
    },
  });

  if (!response.ok) {
    return false;
  }

  const body = (await response.json()) as AuthStatusResponse;
  return body.authenticated;
}
