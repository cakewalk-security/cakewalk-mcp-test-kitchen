import { useEffect } from "react";
import type { ToastMessage } from "./types";
import { TOAST_DURATION_MS } from "./uiStyles";

type ToastStackProps = {
  toasts: ToastMessage[];
  onDismiss: (id: number) => void;
};

function ToastItem({ toast, onDismiss }: { toast: ToastMessage; onDismiss: (id: number) => void }) {
  useEffect(() => {
    const timeoutId = setTimeout(() => onDismiss(toast.id), TOAST_DURATION_MS);
    return () => clearTimeout(timeoutId);
  }, [onDismiss, toast.id]);

  return (
    <div
      role="status"
      className={`rounded-lg border px-4 py-3 text-sm shadow-lg backdrop-blur-sm ${
        toast.tone === "success"
          ? "border-insp-success/30 bg-insp-success-bg text-insp-success-fg"
          : "border-insp-error/40 bg-insp-error-bg text-insp-error"
      }`}
    >
      {toast.text}
    </div>
  );
}

export default function ToastStack({ toasts, onDismiss }: ToastStackProps) {
  if (toasts.length === 0) {
    return null;
  }

  return (
    <div
      aria-live="polite"
      className="pointer-events-none fixed right-4 bottom-4 z-50 flex w-full max-w-sm flex-col gap-2"
    >
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>
  );
}
