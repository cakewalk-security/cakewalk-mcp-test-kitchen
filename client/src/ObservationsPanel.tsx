import { useState, type ReactNode } from "react";
import { BTN_GHOST, BTN_SECONDARY } from "./buttonStyles";
import {
  CLEAR_ALL_OBSERVATIONS_LABEL,
  CLEAR_ALL_OBSERVATIONS_TOOLTIP,
  LOG_STREAM_SECTION_TITLE,
} from "./observationPanelCopy";
import {
  hasActiveObservationFilters,
  OBSERVATION_CATEGORIES,
  OBSERVATION_PHASES,
  observationPrimaryLabel,
  type ObservationFilters,
} from "./observationFilters";
import type { Observation } from "./types";
import Tooltip from "./Tooltip";
import { INPUT, SECTION_TITLE } from "./uiStyles";

const MESSAGE_SEARCH_PLACEHOLDER = "Search...";
const STATUS_OK = "OK";
const STATUS_CANCELLED = "CANCELLED";
const FILTERS_TOGGLE_LABEL = "Filters";

type ObservationsPanelProps = {
  observations: Observation[];
  totalCount: number;
  page: number;
  pageSize: number;
  filters: ObservationFilters;
  hasActiveFilters: boolean;
  streamConnected: boolean;
  recentObservationIds: Set<number>;
  isLoadingObservations: boolean;
  onFiltersChange: (filters: ObservationFilters) => void;
  onApplyFilters: () => void;
  onClearFilters: () => void;
  onPageChange: (page: number) => void;
  onObservationSelect: (observation: Observation) => void;
  onClearObservations?: () => void;
  isClearingObservations?: boolean;
};

function statusLabel(observation: Observation): { text: string; tone: "ok" | "error" | "warn" } {
  if (observation.wasCancelled) {
    return { text: STATUS_CANCELLED, tone: "warn" };
  }

  if (observation.statusCode !== undefined && observation.statusCode >= 400) {
    return { text: String(observation.statusCode), tone: "error" };
  }

  return { text: STATUS_OK, tone: "ok" };
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function FilterField({
  label,
  children,
  className = "",
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <label className={`block min-w-0 ${className}`}>
      <span className="mb-1 block text-[11px] font-medium tracking-wide text-insp-muted uppercase">{label}</span>
      {children}
    </label>
  );
}

const FILTER_INPUT = `${INPUT} mt-0 py-1.5 text-xs`;

const STATUS_TONE: Record<"ok" | "error" | "warn", string> = {
  ok: "bg-insp-success-bg text-insp-success-fg",
  error: "bg-insp-error-bg text-insp-error",
  warn: "bg-insp-warning-bg text-insp-warning-fg",
};

export default function ObservationsPanel({
  observations,
  totalCount,
  page,
  pageSize,
  filters,
  hasActiveFilters,
  streamConnected,
  recentObservationIds,
  isLoadingObservations,
  onFiltersChange,
  onApplyFilters,
  onClearFilters,
  onPageChange,
  onObservationSelect,
  onClearObservations,
  isClearingObservations = false,
}: ObservationsPanelProps) {
  const [filtersOpen, setFiltersOpen] = useState(false);
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const rangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const rangeEnd = Math.min(page * pageSize, totalCount);
  const showFilters = filtersOpen || hasActiveFilters || hasActiveObservationFilters(filters);

  const updateFilter = <K extends keyof ObservationFilters>(key: K, value: ObservationFilters[K]) => {
    onFiltersChange({ ...filters, [key]: value });
  };

  const handleFiltersKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === "Enter") {
      event.preventDefault();
      onApplyFilters();
    }
  };

  return (
    <section className="flex h-full min-h-0 flex-col">
      <div className="flex shrink-0 flex-wrap items-center gap-3 border-b border-insp-border px-4 py-3">
        <div className="min-w-0 flex-1">
          <h2 className={SECTION_TITLE}>{LOG_STREAM_SECTION_TITLE}</h2>
        </div>
        <input
          type="search"
          className={`${INPUT} mt-0 max-w-xs flex-1 py-1.5`}
          placeholder={MESSAGE_SEARCH_PLACEHOLDER}
          value={filters.method}
          onChange={(event) => updateFilter("method", event.target.value)}
          onKeyDown={handleFiltersKeyDown}
          aria-label="Search by method"
        />
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={BTN_SECONDARY}
            onClick={() => setFiltersOpen((open) => !open)}
            aria-expanded={showFilters}
          >
            {FILTERS_TOGGLE_LABEL}
          </button>
          {onClearObservations && (
            <Tooltip label={CLEAR_ALL_OBSERVATIONS_TOOLTIP} placement="bottom">
              <button
                type="button"
                className={BTN_SECONDARY}
                onClick={onClearObservations}
                disabled={totalCount === 0 || isClearingObservations || isLoadingObservations}
              >
                {CLEAR_ALL_OBSERVATIONS_LABEL}
              </button>
            </Tooltip>
          )}
          <span
            className={`inline-flex items-center gap-1.5 text-sm font-medium ${
              streamConnected ? "text-insp-success-fg" : "text-insp-warning-fg"
            }`}
          >
            <span
              className={`size-2 rounded-full ${streamConnected ? "bg-insp-success" : "bg-insp-warning"}`}
              aria-hidden
            />
            {streamConnected ? "Live" : "Reconnecting"}
          </span>
        </div>
      </div>

      {showFilters && (
        <div className="shrink-0 border-b border-insp-border px-4 py-3" onKeyDown={handleFiltersKeyDown}>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
            <FilterField label="From">
              <input
                type="date"
                className={FILTER_INPUT}
                value={filters.fromDate}
                onChange={(event) => updateFilter("fromDate", event.target.value)}
              />
            </FilterField>
            <FilterField label="To">
              <input
                type="date"
                className={FILTER_INPUT}
                value={filters.toDate}
                onChange={(event) => updateFilter("toDate", event.target.value)}
              />
            </FilterField>
            <FilterField label="Category">
              <select
                className={FILTER_INPUT}
                value={filters.category}
                onChange={(event) =>
                  updateFilter("category", event.target.value as ObservationFilters["category"])
                }
              >
                {OBSERVATION_CATEGORIES.map((category) => (
                  <option key={category.value} value={category.value}>
                    {category.label}
                  </option>
                ))}
              </select>
            </FilterField>
            <FilterField label="Phase">
              <select
                className={FILTER_INPUT}
                value={filters.phase}
                onChange={(event) => updateFilter("phase", event.target.value)}
              >
                <option value="">All phases</option>
                {OBSERVATION_PHASES.map((phase) => (
                  <option key={phase} value={phase}>
                    {phase.replaceAll("_", " ")}
                  </option>
                ))}
              </select>
            </FilterField>
            <FilterField label="Caller">
              <input
                type="text"
                className={FILTER_INPUT}
                placeholder="email fragment"
                value={filters.caller}
                onChange={(event) => updateFilter("caller", event.target.value)}
              />
            </FilterField>
          </div>
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <button type="button" className={BTN_SECONDARY} onClick={onApplyFilters}>
              Apply filters
            </button>
            <button
              type="button"
              className={BTN_GHOST}
              onClick={onClearFilters}
              disabled={!hasActiveFilters && !hasActiveObservationFilters(filters)}
            >
              Clear
            </button>
          </div>
        </div>
      )}

      <div className="relative min-h-0 flex-1 overflow-y-auto">
        {isLoadingObservations && (
          <div className="absolute inset-0 z-20 flex items-center justify-center bg-insp-surface/70">
            <p className="text-sm text-insp-muted">Loading observations…</p>
          </div>
        )}

        {observations.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
            <p className="text-sm font-medium text-insp-body">
              {hasActiveFilters ? "No observations match these filters" : "Waiting for MCP traffic"}
            </p>
            <p className="mt-1 max-w-sm text-xs text-insp-muted">
              {hasActiveFilters
                ? "Try adjusting or clearing the filters above."
                : "Connect MCP Inspector or another client with your PAT. New requests appear here instantly."}
            </p>
          </div>
        ) : (
          <ul className="space-y-2 p-3">
            {observations.map((observation) => {
              const status = statusLabel(observation);
              return (
                <li key={observation.id}>
                  <button
                    type="button"
                    className={`w-full rounded-md border border-insp-border bg-insp-surface px-3 py-2.5 text-left transition-colors hover:bg-insp-gray-0 ${
                      recentObservationIds.has(observation.id) ? "animate-observation-highlight" : ""
                    }`}
                    onClick={() => onObservationSelect(observation)}
                  >
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      <span className="tabular-nums text-insp-muted">{formatTime(observation.occurredAt)}</span>
                      <span className={`rounded px-1.5 py-0.5 text-[10px] font-semibold tracking-wide uppercase ${STATUS_TONE[status.tone]}`}>
                        {status.text}
                      </span>
                      <span className="tabular-nums text-insp-muted">{observation.durationMs}ms</span>
                      <span className="ml-auto rounded bg-insp-gray-0 px-1.5 py-0.5 text-[10px] font-medium tracking-wide text-insp-muted uppercase">
                        {observation.phase.replaceAll("_", " ")}
                      </span>
                    </div>
                    <div className="mt-1.5 flex flex-wrap items-center gap-2">
                      <span className="rounded bg-insp-gray-0 px-1.5 py-0.5 font-mono text-[10px] font-medium tracking-wide text-insp-body uppercase">
                        {observation.method}
                      </span>
                      <span className="truncate text-sm font-semibold text-insp-heading">
                        {observationPrimaryLabel(observation)}
                      </span>
                      {observation.clientProtocolVersion ? (
                        <span className="font-mono text-[10px] text-insp-muted">{observation.clientProtocolVersion}</span>
                      ) : null}
                    </div>
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>

      <div className="flex shrink-0 flex-wrap items-center justify-between gap-3 border-t border-insp-border px-4 py-2.5">
        <p className="text-xs tabular-nums text-insp-muted">
          Showing {rangeStart}–{rangeEnd} of {totalCount}
        </p>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={BTN_SECONDARY}
            disabled={page <= 1 || isLoadingObservations}
            onClick={() => onPageChange(page - 1)}
          >
            Previous
          </button>
          <span className="min-w-[5.5rem] text-center text-xs tabular-nums text-insp-muted">
            Page {page} of {totalPages}
          </span>
          <button
            type="button"
            className={BTN_SECONDARY}
            disabled={page >= totalPages || isLoadingObservations}
            onClick={() => onPageChange(page + 1)}
          >
            Next
          </button>
        </div>
      </div>
    </section>
  );
}
