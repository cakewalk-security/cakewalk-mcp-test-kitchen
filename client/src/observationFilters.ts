export const OBSERVATIONS_QUERY_PAGE = "page";
export const OBSERVATIONS_QUERY_LIMIT = "limit";
export const OBSERVATIONS_QUERY_FROM = "from";
export const OBSERVATIONS_QUERY_TO = "to";
export const OBSERVATIONS_QUERY_METHOD = "method";
export const OBSERVATIONS_QUERY_PHASE = "phase";
export const OBSERVATIONS_QUERY_CATEGORY = "category";
export const OBSERVATIONS_QUERY_CALLER = "caller";

export const OBSERVATIONS_DEFAULT_PAGE = 1;
export const OBSERVATIONS_PAGE_SIZE = 50;

export const OBSERVATION_PHASES = [
  "initialize",
  "server_discover",
  "client_initialized",
  "discovery",
  "notification",
  "tool_execution",
  "resource_read",
  "prompt_get",
  "protocol",
  "malformed",
] as const;

export const OBSERVATION_CATEGORIES = [
  { value: "all", label: "All" },
  { value: "tools", label: "Tools" },
  { value: "resources", label: "Resources" },
  { value: "prompts", label: "Prompts" },
] as const;

export type ObservationCategory = (typeof OBSERVATION_CATEGORIES)[number]["value"];

export type ObservationPhase = (typeof OBSERVATION_PHASES)[number];

export type ObservationFilters = {
  fromDate: string;
  toDate: string;
  method: string;
  phase: string;
  category: ObservationCategory;
  caller: string;
};

export const EMPTY_OBSERVATION_FILTERS: ObservationFilters = {
  fromDate: "",
  toDate: "",
  method: "",
  phase: "",
  category: "all",
  caller: "",
};

function toLocalStartOfDayIso(date: string): string {
  const [year, month, day] = parseIsoDateParts(date);
  return new Date(year, month - 1, day, 0, 0, 0, 0).toISOString();
}

function toLocalEndOfDayIso(date: string): string {
  const [year, month, day] = parseIsoDateParts(date);
  return new Date(year, month - 1, day, 23, 59, 59, 999).toISOString();
}

function parseIsoDateParts(date: string): [number, number, number] {
  const parts = date.split("-").map(Number);
  return [parts[0] ?? 0, parts[1] ?? 0, parts[2] ?? 0];
}

export function buildObservationListQuery(page: number, pageSize: number, filters: ObservationFilters): string {
  const params = new URLSearchParams();
  params.set(OBSERVATIONS_QUERY_PAGE, String(page));
  params.set(OBSERVATIONS_QUERY_LIMIT, String(pageSize));

  if (filters.fromDate) {
    params.set(OBSERVATIONS_QUERY_FROM, toLocalStartOfDayIso(filters.fromDate));
  }

  if (filters.toDate) {
    params.set(OBSERVATIONS_QUERY_TO, toLocalEndOfDayIso(filters.toDate));
  }

  if (filters.method.trim()) {
    params.set(OBSERVATIONS_QUERY_METHOD, filters.method.trim());
  }

  if (filters.phase) {
    params.set(OBSERVATIONS_QUERY_PHASE, filters.phase);
  }

  if (filters.category && filters.category !== "all") {
    params.set(OBSERVATIONS_QUERY_CATEGORY, filters.category);
  }

  if (filters.caller.trim()) {
    params.set(OBSERVATIONS_QUERY_CALLER, filters.caller.trim());
  }

  return params.toString();
}

export function observationBelongsToViewer(
  observation: { callerEmail?: string },
  viewerEmail: string | undefined,
): boolean {
  const viewer = viewerEmail?.trim();
  if (!viewer) {
    return false;
  }

  const caller = observation.callerEmail?.trim();
  if (!caller) {
    return false;
  }

  return caller.toLowerCase() === viewer.toLowerCase();
}

export function observationMatchesFilters(
  observation: {
    occurredAt: string;
    method: string;
    phase: string;
    callerEmail?: string;
  },
  filters: ObservationFilters,
): boolean {
  const occurredAt = new Date(observation.occurredAt).getTime();

  if (filters.fromDate) {
    const from = new Date(toLocalStartOfDayIso(filters.fromDate)).getTime();
    if (occurredAt < from) {
      return false;
    }
  }

  if (filters.toDate) {
    const to = new Date(toLocalEndOfDayIso(filters.toDate)).getTime();
    if (occurredAt > to) {
      return false;
    }
  }

  if (filters.method.trim() && observation.method !== filters.method.trim()) {
    return false;
  }

  if (filters.phase && observation.phase !== filters.phase) {
    return false;
  }

  if (!observationMatchesCategory(observation.method, filters.category)) {
    return false;
  }

  if (filters.caller.trim()) {
    const needle = filters.caller.trim().toLowerCase();
    const haystack = observation.callerEmail?.toLowerCase() ?? "";
    if (!haystack.includes(needle)) {
      return false;
    }
  }

  return true;
}

function observationMatchesCategory(method: string, category: ObservationCategory): boolean {
  if (category === "all") {
    return true;
  }

  if (category === "tools") {
    return method.startsWith("tools/");
  }

  if (category === "resources") {
    return method.startsWith("resources/");
  }

  if (category === "prompts") {
    return method.startsWith("prompts/");
  }

  return true;
}

export function observationPrimaryLabel(observation: {
  method: string;
  scenarioId?: string;
  requestParamsJson?: string;
}): string {
  if (observation.scenarioId) {
    return observation.scenarioId;
  }

  if (observation.requestParamsJson) {
    try {
      const params = JSON.parse(observation.requestParamsJson) as { uri?: string; name?: string };
      if (params.uri) {
        return params.uri;
      }

      if (params.name) {
        return params.name;
      }
    } catch {
      // Fall through to em dash.
    }
  }

  return "—";
}

export function hasActiveObservationFilters(filters: ObservationFilters): boolean {
  return Boolean(
    filters.fromDate ||
      filters.toDate ||
      filters.method.trim() ||
      filters.phase ||
      (filters.category && filters.category !== "all") ||
      filters.caller.trim(),
  );
}
