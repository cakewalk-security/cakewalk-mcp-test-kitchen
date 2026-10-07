import { useMemo, useState } from "react";
import ScenarioParamsModal from "./ScenarioParamsModal";
import {
  BASELINE_PROMPTS,
  BASELINE_RESOURCES,
  CATALOG_FACETS,
  getCatalogFacetHint,
  getCatalogFacetTitle,
  getCatalogSearchPlaceholder,
  matchesBaselinePromptQuery,
  matchesBaselineResourceQuery,
  type BaselinePromptItem,
  type BaselineResourceItem,
  type CatalogFacet,
} from "./baselineCatalog";
import { BTN_PRIMARY, BTN_SECONDARY } from "./buttonStyles";
import {
  findScenario,
  formatParamsJson,
  getScenarioAreaLabel,
  isParamsDirty,
} from "./scenarioDisplay";
import { isValidScenarioParamsJson } from "./scenarioPicker";
import Tooltip from "./Tooltip";
import type { ScenarioMetadata, UserScenarioSelection } from "./types";
import { INPUT, SECTION_HINT, SECTION_TITLE } from "./uiStyles";

const SCENARIO_LIST_ID = "scenario-list";
const RESOURCE_LIST_ID = "resource-list";
const PROMPT_LIST_ID = "prompt-list";

type ControlPanelProps = {
  scenarios: ScenarioMetadata[];
  userScenario: UserScenarioSelection | null;
  liveSessionCount: number;
  selectedScenarioId: string;
  selectedParamsJson: string;
  isPending: boolean;
  mcpStateless: boolean;
  onSelectedScenarioIdChange: (value: string) => void;
  onSelectedParamsJsonChange: (value: string) => void;
  onSaveScenario: () => void;
  onTerminateSessions: () => void;
};

function matchesScenarioQuery(scenario: ScenarioMetadata, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack =
    `${scenario.title} ${scenario.id} ${getScenarioAreaLabel(scenario.area)} ${scenario.articleUrl ?? ""}`.toLowerCase();
  return haystack.includes(query);
}

function CatalogFacetTabs({
  value,
  onChange,
}: {
  value: CatalogFacet;
  onChange: (facet: CatalogFacet) => void;
}) {
  return (
    <div className="inline-flex rounded-md bg-insp-gray-0 p-1" role="tablist" aria-label="MCP catalog">
      {CATALOG_FACETS.map((facet) => {
        const selected = value === facet.id;
        return (
          <button
            key={facet.id}
            type="button"
            role="tab"
            aria-selected={selected}
            className={`rounded-[5px] px-3 py-1 text-sm font-medium transition-colors ${
              selected
                ? "bg-insp-surface text-insp-heading shadow-sm"
                : "text-insp-muted hover:text-insp-heading"
            }`}
            onClick={() => onChange(facet.id)}
          >
            {facet.label}
          </button>
        );
      })}
    </div>
  );
}

function CatalogListButton({
  title,
  subtitle,
  selected,
  disabled,
  onClick,
}: {
  title: string;
  subtitle: string;
  selected: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      role="option"
      aria-selected={selected}
      disabled={disabled}
      className={`flex w-full flex-col items-start rounded-md px-3 py-2 text-left transition-colors ${
        selected
          ? "bg-insp-primary-soft text-insp-heading"
          : "text-insp-heading hover:bg-insp-gray-0"
      }`}
      onClick={onClick}
    >
      <span className="text-sm font-medium">{title}</span>
      <span className="mt-0.5 text-[11px] text-insp-muted">{subtitle}</span>
    </button>
  );
}

function ResourceDetail({ resource }: { resource: BaselineResourceItem }) {
  return (
    <div>
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <span className="rounded-full bg-insp-gray-0 px-2 py-0.5 font-mono text-[11px] text-insp-muted">
          Always on
        </span>
        <span className="font-mono text-[11px] text-insp-muted">{resource.name}</span>
      </div>
      <p className="mt-1 font-mono text-[11px] text-insp-muted">{resource.uri}</p>
      <p className="mt-1 text-sm leading-snug text-insp-muted">{resource.description}</p>
      <p className="mt-2 text-sm leading-snug text-insp-body">{resource.usage}</p>
      <p className="mt-2 text-sm leading-snug text-insp-muted">
        MIME type <span className="font-mono text-[11px]">{resource.mimeType}</span>. No scenario configuration —
        available on every session.
      </p>
    </div>
  );
}

function PromptDetail({ prompt }: { prompt: BaselinePromptItem }) {
  return (
    <div>
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <span className="rounded-full bg-insp-gray-0 px-2 py-0.5 font-mono text-[11px] text-insp-muted">
          Always on
        </span>
        <span className="font-mono text-[11px] text-insp-muted">{prompt.name}</span>
      </div>
      <p className="mt-1 text-sm leading-snug text-insp-muted">{prompt.description}</p>
      <p className="mt-2 text-sm leading-snug text-insp-body">{prompt.usage}</p>
      {prompt.arguments && prompt.arguments.length > 0 ? (
        <ul className="mt-2 space-y-1 text-sm text-insp-muted">
          {prompt.arguments.map((argument) => (
            <li key={argument.name}>
              <span className="font-mono text-[11px]">{argument.name}</span>
              {argument.required ? " (required)" : " (optional)"} — {argument.description}
            </li>
          ))}
        </ul>
      ) : null}
      <p className="mt-2 text-sm leading-snug text-insp-muted">No scenario configuration — available on every session.</p>
    </div>
  );
}

export default function ControlPanel({
  scenarios,
  userScenario,
  liveSessionCount,
  selectedScenarioId,
  selectedParamsJson,
  isPending,
  mcpStateless,
  onSelectedScenarioIdChange,
  onSelectedParamsJsonChange,
  onSaveScenario,
  onTerminateSessions,
}: ControlPanelProps) {
  const [paramsModalOpen, setParamsModalOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [catalogFacet, setCatalogFacet] = useState<CatalogFacet>("tools");
  const [selectedResourceName, setSelectedResourceName] = useState(BASELINE_RESOURCES[0]?.name ?? "");
  const [selectedPromptName, setSelectedPromptName] = useState(BASELINE_PROMPTS[0]?.name ?? "");

  const selectedScenario = findScenario(scenarios, selectedScenarioId);
  const savedScenario = findScenario(scenarios, userScenario?.scenarioId ?? "");
  const paramsInvalid = !isValidScenarioParamsJson(selectedParamsJson);
  const paramsDirty =
    selectedScenario !== undefined && isParamsDirty(selectedParamsJson, selectedScenario.paramsExampleJson);
  const selectionDiffersFromSaved =
    userScenario !== null &&
    (userScenario.scenarioId !== selectedScenarioId ||
      formatParamsJson(userScenario.paramsJson) !== formatParamsJson(selectedParamsJson));
  const isSavedForNextSession = !selectionDiffersFromSaved && !paramsInvalid && userScenario !== null;

  const normalizedQuery = query.trim().toLowerCase();
  const visibleScenarios = useMemo(
    () => scenarios.filter((scenario) => matchesScenarioQuery(scenario, normalizedQuery)),
    [normalizedQuery, scenarios],
  );
  const visibleResources = useMemo(
    () => BASELINE_RESOURCES.filter((resource) => matchesBaselineResourceQuery(resource, normalizedQuery)),
    [normalizedQuery],
  );
  const visiblePrompts = useMemo(
    () => BASELINE_PROMPTS.filter((prompt) => matchesBaselinePromptQuery(prompt, normalizedQuery)),
    [normalizedQuery],
  );

  const selectedResource =
    BASELINE_RESOURCES.find((resource) => resource.name === selectedResourceName) ?? BASELINE_RESOURCES[0];
  const selectedPrompt = BASELINE_PROMPTS.find((prompt) => prompt.name === selectedPromptName) ?? BASELINE_PROMPTS[0];

  const listId =
    catalogFacet === "tools"
      ? SCENARIO_LIST_ID
      : catalogFacet === "resources"
        ? RESOURCE_LIST_ID
        : PROMPT_LIST_ID;

  const emptyListMessage =
    catalogFacet === "tools"
      ? "No scenarios match that search."
      : catalogFacet === "resources"
        ? "No resources match that search."
        : "No prompts match that search.";

  return (
    <>
      <div className="flex h-full min-h-0 flex-col">
        <div className="shrink-0 border-b border-insp-border px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className={SECTION_TITLE}>{getCatalogFacetTitle(catalogFacet)}</h2>
            {catalogFacet === "tools" && liveSessionCount > 0 && (
              <span className="rounded-full bg-insp-primary-soft px-2 py-0.5 text-[11px] font-medium text-insp-heading">
                {liveSessionCount} live
              </span>
            )}
          </div>
          <p className={SECTION_HINT}>{getCatalogFacetHint(catalogFacet, mcpStateless)}</p>
          <div className="mt-3">
            <CatalogFacetTabs value={catalogFacet} onChange={setCatalogFacet} />
          </div>
          <input
            type="search"
            className={`${INPUT} mt-3`}
            placeholder={getCatalogSearchPlaceholder(catalogFacet)}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            aria-controls={listId}
          />
        </div>

        {catalogFacet === "tools" && liveSessionCount > 0 && (
          <p className="shrink-0 border-b border-insp-warning/30 bg-insp-warning-bg px-4 py-2 text-xs text-insp-warning-fg">
            A client may still be connected. Save, then terminate sessions to apply immediately.
          </p>
        )}

        <div
          id={listId}
          className="min-h-0 flex-1 overflow-y-auto p-2"
          role="listbox"
          aria-label={getCatalogFacetTitle(catalogFacet)}
        >
          {catalogFacet === "tools" && (
            <>
              {visibleScenarios.length === 0 ? (
                <p className="px-2 py-6 text-center text-sm text-insp-muted">{emptyListMessage}</p>
              ) : (
                visibleScenarios.map((scenario) => {
                  const selected = scenario.id === selectedScenarioId;
                  return (
                    <CatalogListButton
                      key={scenario.id}
                      title={scenario.title}
                      subtitle={getScenarioAreaLabel(scenario.area)}
                      selected={selected}
                      disabled={isPending}
                      onClick={() => onSelectedScenarioIdChange(scenario.id)}
                    />
                  );
                })
              )}
            </>
          )}

          {catalogFacet === "resources" && (
            <>
              {visibleResources.length === 0 ? (
                <p className="px-2 py-6 text-center text-sm text-insp-muted">{emptyListMessage}</p>
              ) : (
                visibleResources.map((resource) => (
                  <CatalogListButton
                    key={resource.name}
                    title={resource.title}
                    subtitle={resource.uri}
                    selected={resource.name === selectedResourceName}
                    onClick={() => setSelectedResourceName(resource.name)}
                  />
                ))
              )}
            </>
          )}

          {catalogFacet === "prompts" && (
            <>
              {visiblePrompts.length === 0 ? (
                <p className="px-2 py-6 text-center text-sm text-insp-muted">{emptyListMessage}</p>
              ) : (
                visiblePrompts.map((prompt) => (
                  <CatalogListButton
                    key={prompt.name}
                    title={prompt.title}
                    subtitle={prompt.name}
                    selected={prompt.name === selectedPromptName}
                    onClick={() => setSelectedPromptName(prompt.name)}
                  />
                ))
              )}
            </>
          )}
        </div>

        <div className="shrink-0 border-t border-insp-border px-4 py-3">
          {catalogFacet === "tools" && selectedScenario && (
            <div className="mb-3">
              <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                {mcpStateless && (
                  <span className="rounded-full bg-insp-gray-0 px-2 py-0.5 font-mono text-[11px] text-insp-muted">
                    Stateless = true
                  </span>
                )}
                {paramsDirty && (
                  <span className="text-[11px] font-medium tracking-wide text-insp-muted uppercase">
                    Custom params
                  </span>
                )}
                <span className="font-mono text-[11px] text-insp-muted">{selectedScenario.id}</span>
              </div>
              <p className="mt-1 text-sm leading-snug text-insp-muted">{selectedScenario.description}</p>
              {selectedScenario.articleUrl && (
                <a
                  href={selectedScenario.articleUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-1.5 inline-block text-sm text-insp-primary hover:underline"
                >
                  Announcing v2.0 of the official MCP C# SDK
                </a>
              )}
            </div>
          )}

          {catalogFacet === "resources" && selectedResource && <ResourceDetail resource={selectedResource} />}
          {catalogFacet === "prompts" && selectedPrompt && <PromptDetail prompt={selectedPrompt} />}

          {catalogFacet === "tools" && (
            <>
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className={BTN_SECONDARY}
                  disabled={isPending || !selectedScenario}
                  onClick={() => setParamsModalOpen(true)}
                >
                  Customize
                </button>

                <Tooltip label="Save scenario for your next MCP session">
                  <button
                    type="button"
                    className={BTN_PRIMARY}
                    disabled={isPending || paramsInvalid || !selectionDiffersFromSaved}
                    onClick={onSaveScenario}
                  >
                    Save
                  </button>
                </Tooltip>

                {liveSessionCount > 0 && (
                  <Tooltip label="End live MCP sessions so the new scenario takes effect immediately">
                    <button
                      type="button"
                      className={BTN_SECONDARY}
                      disabled={isPending}
                      onClick={onTerminateSessions}
                    >
                      Terminate
                    </button>
                  </Tooltip>
                )}
              </div>

              {isSavedForNextSession && savedScenario && (
                <p className="mt-2 text-sm leading-snug text-insp-success-fg">
                  <span className="font-medium">{savedScenario.title}</span> is active for your next session.
                </p>
              )}

              {selectionDiffersFromSaved && !paramsInvalid && (
                <p className="mt-2 text-sm leading-snug text-insp-muted">
                  Unsaved changes — save to apply next session.
                </p>
              )}
            </>
          )}
        </div>
      </div>

      <ScenarioParamsModal
        open={paramsModalOpen}
        scenario={selectedScenario}
        paramsJson={selectedParamsJson}
        isPending={isPending}
        onClose={() => setParamsModalOpen(false)}
        onApply={onSelectedParamsJsonChange}
      />
    </>
  );
}
