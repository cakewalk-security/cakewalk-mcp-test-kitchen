export const ERASE_EVERYTHING_CONFIRMATION_PHRASE = "erase everything";

export const ERASE_EVERYTHING_TITLE = "Erase everything";

export const ERASE_EVERYTHING_HINT =
  "This permanently deletes your observations, saved scenario, MCP access token, and live sessions. You will be signed out. This cannot be undone.";

export const ERASE_EVERYTHING_INPUT_LABEL = `Type ${ERASE_EVERYTHING_CONFIRMATION_PHRASE} to confirm`;

export const ERASE_EVERYTHING_MISMATCH_ERROR = `Type ${ERASE_EVERYTHING_CONFIRMATION_PHRASE} exactly to continue.`;

export const ERASE_EVERYTHING_SUBMIT_ERROR = "Could not erase your data. Please try again.";

export const ERASE_EVERYTHING_TOOLTIP = "Erase everything";

export function isEraseEverythingConfirmation(value: string): boolean {
  return value.trim() === ERASE_EVERYTHING_CONFIRMATION_PHRASE;
}
