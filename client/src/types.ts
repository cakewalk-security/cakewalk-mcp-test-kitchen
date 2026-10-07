export type ScenarioMetadata = {
  id: string;
  title: string;
  description: string;
  area: string;
  paramsExampleJson: string;
  articleUrl?: string;
};

export type UserScenarioSelection = {
  scenarioId: string;
  paramsJson: string;
  updatedAt: string;
  appliesToMessage: string;
};

export type ScenarioSession = {
  mcpSessionId?: string;
  scenarioId: string;
  paramsJson: string;
  startedAt: string;
  invocationCounts: Record<string, number>;
};

export type Observation = {
  id: number;
  requestId?: string;
  callerEmail?: string;
  scenarioId?: string;
  method: string;
  phase: string;
  durationMs: number;
  statusCode?: number;
  wasCancelled: boolean;
  headersJson?: string;
  requestParamsJson?: string;
  clientProtocolVersion?: string;
  occurredAt: string;
};

export type ObservationListResponse = {
  items: Observation[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type UserMcpPat = {
  email: string;
  pat: string;
};

export type CurrentUser = {
  email?: string;
  googleLoginEnabled: boolean;
  githubLoginEnabled: boolean;
  mcpProtocolVersion: string;
  mcpStateless: boolean;
  canAccessAdmin?: boolean;
};

export type AdminUsageTotals = {
  registeredUserCount: number;
  connectedUserCount: number;
  liveSessionCount: number;
  activeUsersLast24Hours: number;
  observationsLast24Hours: number;
  observationsLast7Days: number;
};

export type AdminUsageUserRow = {
  maskedEmail: string;
  registeredAt?: string;
  lastSeenAt?: string;
  observationCountLast24Hours: number;
  observationCountLast7Days: number;
  liveSessionCount: number;
  lastScenarioId?: string;
};

export type AdminUsageResponse = {
  totals: AdminUsageTotals;
  users: AdminUsageUserRow[];
};

export type RuntimeState = {
  scenarioId: string;
  paramsJson: string;
  liveSessions: ScenarioSession[];
};

export type ToastMessage = {
  id: number;
  tone: "success" | "error";
  text: string;
};
