export const STORAGE_KEY_THEME = "mcp_test_server_theme";

export type Theme = "light" | "dark";

export function resolveTheme(stored: string | null): Theme {
  if (stored === "light" || stored === "dark") {
    return stored;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function getStoredTheme(): Theme {
  return resolveTheme(localStorage.getItem(STORAGE_KEY_THEME));
}

export function applyTheme(theme: Theme): void {
  document.documentElement.classList.toggle("dark", theme === "dark");
}

export function setTheme(theme: Theme): void {
  localStorage.setItem(STORAGE_KEY_THEME, theme);
  applyTheme(theme);
}
