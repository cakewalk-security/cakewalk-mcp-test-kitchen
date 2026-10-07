import { useMemo, useState } from "react";
import { BTN_SECONDARY } from "./buttonStyles";
import {
  getTestClientLanguages,
  TEST_CLIENT_LANGUAGE_TYPESCRIPT,
  type TestClientLanguage,
} from "./testClientSources";
import { SECTION_HINT, SECTION_TITLE } from "./uiStyles";

type TestClientSectionProps = {
  origin: string;
};

const LANGUAGE_TAB_ACTIVE =
  "rounded-md border border-insp-primary/40 bg-insp-primary-soft px-3 py-1.5 text-xs font-medium text-insp-heading";
const LANGUAGE_TAB_INACTIVE =
  "rounded-md border border-transparent px-3 py-1.5 text-xs font-medium text-insp-muted hover:text-insp-heading";
const LANGUAGE_TAB_DISABLED =
  "cursor-not-allowed rounded-lg border border-transparent px-3 py-1.5 text-xs font-medium text-insp-muted/50";
const SECTION_TOGGLE =
  "flex w-full items-start gap-2 rounded-md text-left transition hover:bg-insp-gray-0";

export default function TestClientSection({ origin }: TestClientSectionProps) {
  const languages = useMemo(() => getTestClientLanguages(), []);
  const defaultLanguage =
    languages.find((language) => language.id === TEST_CLIENT_LANGUAGE_TYPESCRIPT) ?? languages[0];

  const [isExpanded, setIsExpanded] = useState(false);
  const [selectedLanguageId, setSelectedLanguageId] = useState(defaultLanguage.id);

  const selectedLanguage =
    languages.find((language) => language.id === selectedLanguageId) ?? defaultLanguage;

  const handleLanguageSelect = (language: TestClientLanguage) => {
    if (!language.available) {
      return;
    }

    setSelectedLanguageId(language.id);
  };

  return (
    <div className="space-y-3">
      <button
        type="button"
        className={SECTION_TOGGLE}
        aria-expanded={isExpanded}
        aria-controls="test-client-section-content"
        onClick={() => setIsExpanded((expanded) => !expanded)}
      >
        <svg
          viewBox="0 0 20 20"
          className={`mt-0.5 size-4 shrink-0 text-insp-muted transition-transform ${isExpanded ? "rotate-90" : ""}`}
          fill="currentColor"
          aria-hidden
        >
          <path d="M7.21 14.77a.75.75 0 0 1 .02-1.06L10.94 10 7.23 6.29a.75.75 0 1 1 1.04-1.08l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 0 1-1.06-.02Z" />
        </svg>
        <div>
          <h3 className={SECTION_TITLE}>Test clients (2026-07-28)</h3>
          <p className={SECTION_HINT}>
            Reference MCP clients that pin the native protocol. Clone the public TypeScript client and run it locally.
          </p>
        </div>
      </button>

      {isExpanded ? (
        <div id="test-client-section-content" className="space-y-3">
          <div className="flex flex-wrap gap-2" role="tablist" aria-label="Test client languages">
            {languages.map((language) => {
              const isSelected = language.id === selectedLanguageId;
              const className = !language.available
                ? LANGUAGE_TAB_DISABLED
                : isSelected
                  ? LANGUAGE_TAB_ACTIVE
                  : LANGUAGE_TAB_INACTIVE;

              return (
                <button
                  key={language.id}
                  type="button"
                  role="tab"
                  aria-selected={isSelected}
                  disabled={!language.available}
                  className={className}
                  onClick={() => handleLanguageSelect(language)}
                >
                  {language.label}
                  {!language.available ? " (soon)" : null}
                </button>
              );
            })}
          </div>

          {selectedLanguage.available && selectedLanguage.repoUrl ? (
            <>
              <p className="text-sm text-insp-muted">
                Set <code className="font-mono">MCP_URL</code> to{" "}
                <code className="font-mono">{origin}</code> and{" "}
                <code className="font-mono">MCP_PAT</code> to your personal PAT from above.
              </p>
              <a
                className={BTN_SECONDARY}
                href={selectedLanguage.repoUrl}
                target="_blank"
                rel="noopener noreferrer"
              >
                Open {selectedLanguage.label} client on GitHub
              </a>
            </>
          ) : (
            <p className="text-sm text-insp-muted">{selectedLanguage.comingSoonMessage}</p>
          )}
        </div>
      ) : null}
    </div>
  );
}
