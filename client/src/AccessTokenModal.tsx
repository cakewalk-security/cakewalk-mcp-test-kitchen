import { useEffect, useMemo } from "react";
import { BTN_GHOST, BTN_PRIMARY, BTN_SECONDARY } from "./buttonStyles";
import { buildMcpClientConfigJson } from "./mcpClientConfig";
import { displayPat } from "./demoMode";
import TestClientSection from "./TestClientSection";
import Tooltip from "./Tooltip";
import type { UserMcpPat } from "./types";
import { CARD, DIVIDER, INPUT, MODAL_BACKDROP, SECTION_HINT, SECTION_TITLE } from "./uiStyles";

type AccessTokenModalProps = {
  open: boolean;
  userPat: UserMcpPat | null;
  isPending: boolean;
  onClose: () => void;
  onCopyPat: () => void;
  onCopyClientConfig: () => void;
  onRegeneratePat: () => void;
};

export default function AccessTokenModal({
  open,
  userPat,
  isPending,
  onClose,
  onCopyPat,
  onCopyClientConfig,
  onRegeneratePat,
}: AccessTokenModalProps) {
  const clientConfigJson = useMemo(() => {
    if (!userPat) {
      return "";
    }

    return buildMcpClientConfigJson(displayPat(userPat.pat), window.location.origin);
  }, [userPat]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        className={MODAL_BACKDROP}
        aria-label="Close access token dialog"
        onClick={onClose}
      />

      <div
        className={`${CARD} relative max-h-[90vh] w-full max-w-3xl overflow-y-auto p-5 shadow-2xl`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="access-token-title"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 id="access-token-title" className={SECTION_TITLE}>
              MCP access token
            </h2>
            <p className={SECTION_HINT}>Bearer token for Cursor and other MCP clients.</p>
          </div>
          <button type="button" className={BTN_GHOST} onClick={onClose}>
            Close
          </button>
        </div>

        {userPat ? (
          <div className="mt-5 space-y-5">
            <div className="space-y-4">
              <input
                className={`${INPUT} mt-0 font-mono text-xs`}
                readOnly
                value={displayPat(userPat.pat)}
                aria-label="MCP personal access token"
                onFocus={(event) => event.currentTarget.select()}
              />
              <div className="flex flex-wrap gap-2">
                <Tooltip label="Copy token to clipboard">
                  <button type="button" className={BTN_SECONDARY} disabled={isPending} onClick={onCopyPat}>
                    Copy token
                  </button>
                </Tooltip>
                <Tooltip label="Invalidate current token and create a new one">
                  <button type="button" className={BTN_PRIMARY} disabled={isPending} onClick={onRegeneratePat}>
                    Regenerate
                  </button>
                </Tooltip>
              </div>
            </div>

            <div className={DIVIDER} />

            <div className="space-y-3">
              <div>
                <h3 className={SECTION_TITLE}>Add to Cursor or Claude</h3>
                <p className={SECTION_HINT}>
                  Paste into Cursor Settings → MCP, or merge into your Claude{" "}
                  <code className="rounded bg-insp-gray-0 px-1 py-0.5 font-mono text-xs text-insp-body">
                    claude_desktop_config.json
                  </code>
                  .
                </p>
              </div>

              <textarea
                className={`${INPUT} mt-0 min-h-44 resize-y font-mono text-xs leading-relaxed`}
                readOnly
                value={clientConfigJson}
                aria-label="MCP client configuration JSON"
                onFocus={(event) => event.currentTarget.select()}
              />

              <Tooltip label="Copy full MCP client configuration JSON">
                <button
                  type="button"
                  className={BTN_SECONDARY}
                  disabled={isPending}
                  onClick={onCopyClientConfig}
                >
                  Copy config
                </button>
              </Tooltip>
            </div>

            <div className={DIVIDER} />

            <TestClientSection origin={window.location.origin} />
          </div>
        ) : (
          <p className="mt-5 text-sm text-insp-muted">Loading token…</p>
        )}
      </div>
    </div>
  );
}
