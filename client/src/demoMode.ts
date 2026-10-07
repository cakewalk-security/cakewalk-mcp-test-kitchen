export const DEMO_EMAIL = "kitchen@example.com";

export const DEMO_PAT = "mcp_pat_demo_not_a_real_token";

const DEMO_QUERY = "demo";
const DEMO_STORAGE_KEY = "mcp_test_kitchen_demo";

export function isDemoMode(): boolean {
  const params = new URLSearchParams(window.location.search);
  if (params.get(DEMO_QUERY) === "1") {
    return true;
  }

  return localStorage.getItem(DEMO_STORAGE_KEY) === "1";
}

export function displayEmail(email: string | undefined, fallback: string): string {
  if (isDemoMode()) {
    return DEMO_EMAIL;
  }

  return email ?? fallback;
}

export function displayPat(pat: string): string {
  return isDemoMode() ? DEMO_PAT : pat;
}
