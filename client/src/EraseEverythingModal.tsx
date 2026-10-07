import { useEffect, useState } from "react";
import { BTN_DANGER, BTN_GHOST } from "./buttonStyles";
import {
  ERASE_EVERYTHING_CONFIRMATION_PHRASE,
  ERASE_EVERYTHING_HINT,
  ERASE_EVERYTHING_INPUT_LABEL,
  ERASE_EVERYTHING_MISMATCH_ERROR,
  ERASE_EVERYTHING_TITLE,
  isEraseEverythingConfirmation,
} from "./eraseEverything";
import { CARD, ERROR_TEXT, INPUT, LABEL, MODAL_BACKDROP, SECTION_HINT, SECTION_TITLE } from "./uiStyles";

type EraseEverythingModalProps = {
  open: boolean;
  isPending: boolean;
  onClose: () => void;
  onConfirm: (confirmation: string) => Promise<void>;
};

export default function EraseEverythingModal({
  open,
  isPending,
  onClose,
  onConfirm,
}: EraseEverythingModalProps) {
  const [confirmation, setConfirmation] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setConfirmation("");
      setValidationError(null);
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isPending) {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [open, isPending, onClose]);

  if (!open) {
    return null;
  }

  const canSubmit = isEraseEverythingConfirmation(confirmation) && !isPending;

  const handleSubmit = async () => {
    if (!isEraseEverythingConfirmation(confirmation)) {
      setValidationError(ERASE_EVERYTHING_MISMATCH_ERROR);
      return;
    }

    setValidationError(null);
    await onConfirm(confirmation.trim());
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        className={MODAL_BACKDROP}
        aria-label="Close erase everything dialog"
        disabled={isPending}
        onClick={onClose}
      />

      <div
        className={`${CARD} relative w-full max-w-lg p-5 shadow-2xl`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="erase-everything-title"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 id="erase-everything-title" className={SECTION_TITLE}>
              {ERASE_EVERYTHING_TITLE}
            </h2>
            <p className={SECTION_HINT}>{ERASE_EVERYTHING_HINT}</p>
          </div>
          <button type="button" className={BTN_GHOST} disabled={isPending} onClick={onClose}>
            Close
          </button>
        </div>

        <div className="mt-5 space-y-4">
          <div>
            <label htmlFor="erase-everything-confirmation" className={LABEL}>
              {ERASE_EVERYTHING_INPUT_LABEL}
            </label>
            <input
              id="erase-everything-confirmation"
              className={INPUT}
              value={confirmation}
              autoComplete="off"
              spellCheck={false}
              autoFocus
              disabled={isPending}
              placeholder={ERASE_EVERYTHING_CONFIRMATION_PHRASE}
              onChange={(event) => {
                setConfirmation(event.target.value);
                if (validationError) {
                  setValidationError(null);
                }
              }}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void handleSubmit();
                }
              }}
            />
            {validationError ? (
              <p className={`mt-1.5 ${ERROR_TEXT}`} role="alert">
                {validationError}
              </p>
            ) : null}
          </div>

          <div className="flex flex-wrap justify-end gap-2">
            <button type="button" className={BTN_GHOST} disabled={isPending} onClick={onClose}>
              Cancel
            </button>
            <button
              type="button"
              className={BTN_DANGER}
              disabled={!canSubmit}
              onClick={() => void handleSubmit()}
            >
              {isPending ? "Erasing…" : ERASE_EVERYTHING_TITLE}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
