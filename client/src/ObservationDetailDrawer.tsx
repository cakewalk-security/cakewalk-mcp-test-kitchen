import { useEffect, useState } from "react";
import { BTN_ICON } from "./buttonStyles";
import type { Observation } from "./types";
import { CODE_BLOCK, SECTION_TITLE } from "./uiStyles";

type ObservationDetailDrawerProps = {
  observation: Observation | null;
  onClose: () => void;
};

function formatJson(value?: string): string {
  if (!value) {
    return "—";
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

export default function ObservationDetailDrawer({ observation, onClose }: ObservationDetailDrawerProps) {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    if (observation) {
      setOpen(true);
    }
  }, [observation]);

  if (!observation) {
    return null;
  }

  const handleClose = () => {
    setOpen(false);
    onClose();
  };

  return (
    <div
      className={`fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col border-l border-insp-border bg-insp-surface shadow-xl transition-transform ${
        open ? "translate-x-0" : "translate-x-full"
      }`}
      role="dialog"
      aria-label="Message details"
    >
      <div className="flex items-center gap-2 border-b border-insp-border px-3 py-2.5">
        <button type="button" className={BTN_ICON} onClick={handleClose} aria-label="Close results">
          <svg viewBox="0 0 20 20" className="size-4" fill="currentColor" aria-hidden>
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
          </svg>
        </button>
        <h3 className={SECTION_TITLE}>Results</h3>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 text-sm">
        <dl className="space-y-4">
          <div>
            <dt className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">Request ID</dt>
            <dd className="mt-1 font-mono text-xs">{observation.requestId ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">Client protocol</dt>
            <dd className="mt-1 font-mono text-xs">{observation.clientProtocolVersion ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">Status code</dt>
            <dd className="mt-1 tabular-nums">{observation.statusCode ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">Cancelled</dt>
            <dd className="mt-1">{observation.wasCancelled ? "Yes" : "No"}</dd>
          </div>
          <div>
            <dt className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">Headers (redacted)</dt>
            <dd className={`mt-1 ${CODE_BLOCK}`}>
              <pre>{formatJson(observation.headersJson)}</pre>
            </dd>
          </div>
          <div>
            <dt className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">Request params</dt>
            <dd className={`mt-1 ${CODE_BLOCK}`}>
              <pre>{formatJson(observation.requestParamsJson)}</pre>
            </dd>
          </div>
        </dl>
      </div>
    </div>
  );
}
