export const STORAGE_KEY_MESSAGES_PANE_WIDTH = "mcp_test_server_messages_pane_width";

export const MESSAGES_PANE_MIN_WIDTH_PX = 320;

export const MESSAGES_PANE_MAX_WIDTH_PX = 1600;

export const MESSAGES_PANE_DEFAULT_WIDTH_PX = 560;

export const SCENARIOS_PANE_MIN_WIDTH_PX = 220;

export const RESIZE_HANDLE_WIDTH_PX = 6;

export const RESIZE_HANDLE_STEP_PX = 16;

export const MESSAGES_PANE_WIDTH_VAR = "--messages-pane-width";

export const RESIZE_HANDLE_ARIA_LABEL = "Resize messages pane";

export const BODY_RESIZING_CLASS = "resizing-col";

export function clampMessagesPaneWidth(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

export function parseStoredMessagesPaneWidth(raw: string | null): number {
  if (raw === null) {
    return MESSAGES_PANE_DEFAULT_WIDTH_PX;
  }

  const parsed = Number(raw);
  if (!Number.isFinite(parsed)) {
    return MESSAGES_PANE_DEFAULT_WIDTH_PX;
  }

  return clampMessagesPaneWidth(parsed, MESSAGES_PANE_MIN_WIDTH_PX, MESSAGES_PANE_MAX_WIDTH_PX);
}

export function computeMessagesPaneMaxWidth(containerWidth: number): number {
  const available = containerWidth - SCENARIOS_PANE_MIN_WIDTH_PX - RESIZE_HANDLE_WIDTH_PX;
  return Math.max(MESSAGES_PANE_MIN_WIDTH_PX, Math.min(MESSAGES_PANE_MAX_WIDTH_PX, available));
}

export function readMessagesPaneWidth(): number {
  try {
    return parseStoredMessagesPaneWidth(localStorage.getItem(STORAGE_KEY_MESSAGES_PANE_WIDTH));
  } catch {
    return MESSAGES_PANE_DEFAULT_WIDTH_PX;
  }
}

export function writeMessagesPaneWidth(width: number): void {
  try {
    localStorage.setItem(STORAGE_KEY_MESSAGES_PANE_WIDTH, String(width));
  } catch {
    // Ignore quota / privacy-mode failures; the session still resizes.
  }
}
