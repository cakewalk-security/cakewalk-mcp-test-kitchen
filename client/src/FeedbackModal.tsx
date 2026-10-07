import { useEffect, useState } from "react";
import { BTN_GHOST, BTN_PRIMARY } from "./buttonStyles";
import { CARD, ERROR_TEXT, INPUT, MODAL_BACKDROP, SECTION_HINT, SECTION_TITLE } from "./uiStyles";

const FEEDBACK_MAX_LENGTH = 2000;
const FEEDBACK_EMPTY_ERROR = "Please enter your feedback before sending.";
const FEEDBACK_SEND_SUCCESS = "Feedback sent — thank you!";
const FEEDBACK_SEND_ERROR = "Could not send feedback. Please try again.";

type FeedbackModalProps = {
  open: boolean;
  isPending: boolean;
  onClose: () => void;
  onSubmit: (message: string) => Promise<void>;
};

export default function FeedbackModal({ open, isPending, onClose, onSubmit }: FeedbackModalProps) {
  const [message, setMessage] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setMessage("");
      setValidationError(null);
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

  const handleSubmit = async () => {
    const trimmed = message.trim();
    if (!trimmed) {
      setValidationError(FEEDBACK_EMPTY_ERROR);
      return;
    }

    setValidationError(null);
    await onSubmit(trimmed);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        className={MODAL_BACKDROP}
        aria-label="Close feedback dialog"
        onClick={onClose}
      />

      <div
        className={`${CARD} relative w-full max-w-lg p-5 shadow-2xl`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="feedback-title"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 id="feedback-title" className={SECTION_TITLE}>
              Give feedback
            </h2>
            <p className={SECTION_HINT}>Tell us what works, what does not, or what you would like to see next.</p>
          </div>
          <button type="button" className={BTN_GHOST} onClick={onClose}>
            Close
          </button>
        </div>

        <div className="mt-5 space-y-4">
          <div>
            <label htmlFor="feedback-message" className="sr-only">
              Feedback message
            </label>
            <textarea
              id="feedback-message"
              className={`${INPUT} mt-0 min-h-36 resize-y`}
              value={message}
              maxLength={FEEDBACK_MAX_LENGTH}
              placeholder="Share your feedback..."
              disabled={isPending}
              onChange={(event) => {
                setMessage(event.target.value);
                if (validationError) {
                  setValidationError(null);
                }
              }}
            />
            <p className="mt-1.5 text-xs text-insp-muted">
              {message.length}/{FEEDBACK_MAX_LENGTH}
            </p>
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
            <button type="button" className={BTN_PRIMARY} disabled={isPending} onClick={() => void handleSubmit()}>
              {isPending ? "Sending…" : "Send feedback"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export { FEEDBACK_SEND_ERROR, FEEDBACK_SEND_SUCCESS };
