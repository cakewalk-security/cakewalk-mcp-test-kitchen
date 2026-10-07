import { useState } from "react";
import { BTN_ICON } from "./buttonStyles";
import { getStoredTheme, setTheme, type Theme } from "./theme";
import Tooltip from "./Tooltip";

function SunIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
    </svg>
  );
}

function MoonIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
    </svg>
  );
}

export default function ThemeToggle() {
  const [theme, setThemeState] = useState<Theme>(() => getStoredTheme());

  const toggleTheme = () => {
    const nextTheme: Theme = theme === "dark" ? "light" : "dark";
    setTheme(nextTheme);
    setThemeState(nextTheme);
  };

  const tooltipLabel = theme === "dark" ? "Switch to light mode" : "Switch to dark mode";

  return (
    <Tooltip label={tooltipLabel} placement="bottom">
      <button
        type="button"
        className={BTN_ICON}
        onClick={toggleTheme}
        aria-label={tooltipLabel}
      >
        {theme === "dark" ? <SunIcon /> : <MoonIcon />}
      </button>
    </Tooltip>
  );
}
