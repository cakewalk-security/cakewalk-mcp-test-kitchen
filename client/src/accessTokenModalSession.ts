export const STORAGE_KEY_ACCESS_TOKEN_MODAL_AUTO_SHOWN = "mcp_test_server_access_token_modal_auto_shown";

export function shouldAutoShowAccessTokenModal(): boolean {
  return sessionStorage.getItem(STORAGE_KEY_ACCESS_TOKEN_MODAL_AUTO_SHOWN) !== "1";
}

export function markAccessTokenModalAutoShown(): void {
  sessionStorage.setItem(STORAGE_KEY_ACCESS_TOKEN_MODAL_AUTO_SHOWN, "1");
}

export function clearAccessTokenModalAutoShown(): void {
  sessionStorage.removeItem(STORAGE_KEY_ACCESS_TOKEN_MODAL_AUTO_SHOWN);
}
