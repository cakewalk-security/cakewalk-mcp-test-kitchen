import { buildAccountLoginUrl } from "./appRoutes";

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (response.status === 401) {
    window.location.href = buildAccountLoginUrl(window.location.pathname);
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  const body = await response.text();
  if (!body) {
    return undefined as T;
  }

  return JSON.parse(body) as T;
}
