import { useEffect, useState } from "react";
import { BTN_GHOST, BTN_PRIMARY, BTN_SECONDARY } from "./buttonStyles";
import { formatParamsJson } from "./scenarioDisplay";
import { isValidScenarioParamsJson } from "./scenarioPicker";
import type { ScenarioMetadata } from "./types";
import { CARD, ERROR_TEXT, INPUT, LABEL, MODAL_BACKDROP, SECTION_HINT, SECTION_TITLE } from "./uiStyles";

const INVALID_PARAMS_ERROR = "Params must be a valid JSON object.";

type ScenarioParamsModalProps = {
  open: boolean;
  scenario: ScenarioMetadata | undefined;
  paramsJson: string;
  isPending: boolean;
  onClose: () => void;
  onApply: (paramsJson: string) => void;
};

export default function ScenarioParamsModal({
  open,
  scenario,
  paramsJson,
  isPending,
  onClose,
  onApply,
}: ScenarioParamsModalProps) {
  const [draft, setDraft] = useState(paramsJson);

  useEffect(() => {
    if (!open) {
      return;
    }

    setDraft(paramsJson);

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [open, paramsJson, onClose]);

  if (!open) {
    return null;
  }

  const paramsInvalid = !isValidScenarioParamsJson(draft);

  const loadExample = () => {
    if (!scenario) {
      return;
    }

    setDraft(formatParamsJson(scenario.paramsExampleJson));
  };

  const handleApply = () => {
    if (paramsInvalid) {
      return;
    }

    onApply(formatParamsJson(draft));
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        className={MODAL_BACKDROP}
        aria-label="Close scenario params dialog"
        onClick={onClose}
      />

      <div
        className={`${CARD} relative flex max-h-[90vh] w-full max-w-2xl flex-col p-5 shadow-2xl`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="scenario-params-title"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 id="scenario-params-title" className={SECTION_TITLE}>
              Customize scenario
            </h2>
            <p className={SECTION_HINT}>
              {scenario
                ? `Edit params for ${scenario.title}. Changes apply after you save the scenario.`
                : "Edit scenario parameters as JSON."}
            </p>
          </div>
          <button type="button" className={BTN_GHOST} onClick={onClose}>
            Close
          </button>
        </div>

        <div className="mt-5 flex min-h-0 flex-1 flex-col">
          <div className="flex flex-wrap items-end justify-between gap-2">
            <label className={LABEL} htmlFor="scenario-params-json">
              Params JSON
            </label>
            <button
              type="button"
              className={BTN_GHOST}
              disabled={isPending || !scenario}
              onClick={loadExample}
            >
              Load example
            </button>
          </div>

          <textarea
            id="scenario-params-json"
            className={`${INPUT} mt-1.5 min-h-64 flex-1 resize-y font-mono text-sm leading-relaxed ${
              paramsInvalid ? "border-insp-error focus:border-insp-error focus:ring-insp-error/20" : ""
            }`}
            value={draft}
            disabled={isPending}
            placeholder={scenario?.paramsExampleJson}
            onChange={(event) => setDraft(event.target.value)}
            spellCheck={false}
          />

          {paramsInvalid ? (
            <p className={`mt-2 ${ERROR_TEXT}`} role="alert">
              {INVALID_PARAMS_ERROR}
            </p>
          ) : (
            <p className="mt-2 text-sm text-insp-muted">Valid JSON object.</p>
          )}

          <div className="mt-4 flex flex-wrap justify-end gap-2">
            <button type="button" className={BTN_SECONDARY} disabled={isPending} onClick={onClose}>
              Cancel
            </button>
            <button
              type="button"
              className={BTN_PRIMARY}
              disabled={isPending || paramsInvalid}
              onClick={handleApply}
            >
              Apply params
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
