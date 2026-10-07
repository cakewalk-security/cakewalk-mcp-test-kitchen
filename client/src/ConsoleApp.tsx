import { loadConsoleFonts } from "./consoleFonts";
import { useCallback, useEffect, useMemo, useRef, useState, type CSSProperties } from "react";
import AccessTokenModal from "./AccessTokenModal";
import EraseEverythingModal from "./EraseEverythingModal";
import FeedbackModal, { FEEDBACK_SEND_ERROR, FEEDBACK_SEND_SUCCESS } from "./FeedbackModal";
import { ERASE_EVERYTHING_SUBMIT_ERROR } from "./eraseEverything";
import { buildMcpClientConfigJson } from "./mcpClientConfig";
import {
  CLEAR_ALL_OBSERVATIONS_CONFIRM,
  CLEAR_ALL_OBSERVATIONS_SUCCESS,
} from "./observationPanelCopy";
import { APP_DESCRIPTION, APP_NAME } from "./appBranding";
import { CONSOLE_PATH } from "./appRoutes";
import { displayEmail } from "./demoMode";
import { logoutToLoginPage } from "./logout";
import { apiFetch } from "./api";
import {
  clearAccessTokenModalAutoShown,
  markAccessTokenModalAutoShown,
  shouldAutoShowAccessTokenModal,
} from "./accessTokenModalSession";
import {
  MANAGEMENT_ME_PATH,
  MANAGEMENT_ME_PAT_PATH,
  MANAGEMENT_ME_ERASE_PATH,
  MANAGEMENT_ME_PAT_REGENERATE_PATH,
  MANAGEMENT_ME_SCENARIO_PATH,
  MANAGEMENT_ME_SESSIONS_TERMINATE_PATH,
  MANAGEMENT_OBSERVATIONS_PATH,
  MANAGEMENT_RUNTIME_PATH,
  MANAGEMENT_SCENARIOS_PATH,
  MANAGEMENT_FEEDBACK_PATH,
} from "./apiPaths";
import AppHeader from "./AppHeader";
import ControlPanel from "./ControlPanel";
import ObservationDetailDrawer from "./ObservationDetailDrawer";
import ObservationsPanel from "./ObservationsPanel";
import {
  buildObservationListQuery,
  EMPTY_OBSERVATION_FILTERS,
  hasActiveObservationFilters,
  observationBelongsToViewer,
  observationMatchesFilters,
  OBSERVATIONS_DEFAULT_PAGE,
  OBSERVATIONS_PAGE_SIZE,
  type ObservationFilters,
} from "./observationFilters";
import { runObservationStreamLoop } from "./observationStream";
import { findScenario, formatParamsJson, paramsMatchExample } from "./scenarioDisplay";
import { isValidScenarioParamsJson } from "./scenarioPicker";
import {
  CONSOLE_PANE_MESSAGES,
  CONSOLE_PANE_SCENARIO,
  type ConsolePane,
} from "./consolePanes";
import ResizeHandle from "./ResizeHandle";
import ToastStack from "./ToastStack";
import { WORKSPACE_PANE } from "./uiStyles";
import { usePageMetadata } from "./pageMetadata";
import {
  clampMessagesPaneWidth,
  computeMessagesPaneMaxWidth,
  MESSAGES_PANE_MAX_WIDTH_PX,
  MESSAGES_PANE_MIN_WIDTH_PX,
  MESSAGES_PANE_WIDTH_VAR,
  readMessagesPaneWidth,
  SCENARIOS_PANE_MIN_WIDTH_PX,
  writeMessagesPaneWidth,
} from "./workspaceSplit";
import type {
  CurrentUser,
  Observation,
  ObservationListResponse,
  RuntimeState,
  ScenarioMetadata,
  ToastMessage,
  UserMcpPat,
  UserScenarioSelection,
} from "./types";

function upsertObservation(list: Observation[], observation: Observation, limit: number): Observation[] {
  if (list.some((item) => item.id === observation.id)) {
    return list;
  }

  return [observation, ...list].slice(0, limit);
}

export default function ConsoleApp() {
  usePageMetadata({
    title: APP_NAME,
    description: APP_DESCRIPTION,
    canonicalPath: CONSOLE_PATH,
  });

  useEffect(() => {
    loadConsoleFonts();
  }, []);

  const [scenarios, setScenarios] = useState<ScenarioMetadata[]>([]);
  const [userScenario, setUserScenario] = useState<UserScenarioSelection | null>(null);
  const [selectedScenarioId, setSelectedScenarioId] = useState("baseline");
  const [selectedParamsJson, setSelectedParamsJson] = useState("{}");
  const [observations, setObservations] = useState<Observation[]>([]);
  const [observationTotalCount, setObservationTotalCount] = useState(0);
  const [observationPage, setObservationPage] = useState(OBSERVATIONS_DEFAULT_PAGE);
  const [filterDraft, setFilterDraft] = useState<ObservationFilters>(EMPTY_OBSERVATION_FILTERS);
  const [appliedFilters, setAppliedFilters] = useState<ObservationFilters>(EMPTY_OBSERVATION_FILTERS);
  const [bannerError, setBannerError] = useState<string | null>(null);
  const [streamConnected, setStreamConnected] = useState(false);
  const [userPat, setUserPat] = useState<UserMcpPat | null>(null);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [runtimeState, setRuntimeState] = useState<RuntimeState | null>(null);
  const [selectedObservation, setSelectedObservation] = useState<Observation | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingObservations, setIsLoadingObservations] = useState(false);
  const [isPending, setIsPending] = useState(false);
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const [accessTokenModalOpen, setAccessTokenModalOpen] = useState(false);
  const [feedbackModalOpen, setFeedbackModalOpen] = useState(false);
  const [eraseEverythingModalOpen, setEraseEverythingModalOpen] = useState(false);
  const [mobilePane, setMobilePane] = useState<ConsolePane>(CONSOLE_PANE_SCENARIO);
  const [messagesPanePreferredWidth, setMessagesPanePreferredWidth] = useState(readMessagesPaneWidth);
  const [messagesPaneMaxWidth, setMessagesPaneMaxWidth] = useState(MESSAGES_PANE_MAX_WIDTH_PX);
  const splitRef = useRef<HTMLElement>(null);
  const [recentObservationIds, setRecentObservationIds] = useState<Set<number>>(() => new Set());
  const highlightTimeoutsRef = useRef<Map<number, ReturnType<typeof setTimeout>>>(new Map());
  const toastIdRef = useRef(0);
  const observationPageRef = useRef(observationPage);
  const appliedFiltersRef = useRef(appliedFilters);

  observationPageRef.current = observationPage;
  appliedFiltersRef.current = appliedFilters;

  const pushToast = useCallback((tone: ToastMessage["tone"], text: string) => {
    toastIdRef.current += 1;
    const id = toastIdRef.current;
    setToasts((prev) => [...prev, { id, tone, text }]);
  }, []);

  const dismissToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const markObservationHighlight = useCallback((observationId: number) => {
    setRecentObservationIds((prev) => {
      const next = new Set(prev);
      next.add(observationId);
      return next;
    });

    const existingTimeout = highlightTimeoutsRef.current.get(observationId);
    if (existingTimeout) {
      clearTimeout(existingTimeout);
    }

    const timeoutId = setTimeout(() => {
      setRecentObservationIds((prev) => {
        const next = new Set(prev);
        next.delete(observationId);
        return next;
      });
      highlightTimeoutsRef.current.delete(observationId);
    }, 2500);

    highlightTimeoutsRef.current.set(observationId, timeoutId);
  }, []);

  const loadRuntimeState = useCallback(async () => {
    try {
      const runtime = await apiFetch<RuntimeState>(MANAGEMENT_RUNTIME_PATH);
      setRuntimeState(runtime);
    } catch {
      // Runtime state is optional during initial load failures.
    }
  }, []);

  const loadObservations = useCallback(
    async (page: number, filters: ObservationFilters) => {
      setIsLoadingObservations(true);
      try {
        const query = buildObservationListQuery(page, OBSERVATIONS_PAGE_SIZE, filters);
        const response = await apiFetch<ObservationListResponse>(`${MANAGEMENT_OBSERVATIONS_PATH}?${query}`);
        setObservations(response.items);
        setObservationTotalCount(response.totalCount);
        setObservationPage(response.page);
      } catch (e) {
        if (e instanceof Error && e.message !== "Unauthorized") {
          setBannerError(e.message);
        }
      } finally {
        setIsLoadingObservations(false);
      }
    },
    [],
  );

  const loadManagementData = useCallback(async () => {
    setBannerError(null);
    try {
      const [scenarioList, meResponse, patResponse, selectionResponse] = await Promise.all([
        apiFetch<ScenarioMetadata[]>(MANAGEMENT_SCENARIOS_PATH),
        apiFetch<CurrentUser>(MANAGEMENT_ME_PATH),
        apiFetch<UserMcpPat>(MANAGEMENT_ME_PAT_PATH),
        apiFetch<UserScenarioSelection>(MANAGEMENT_ME_SCENARIO_PATH),
      ]);
      setScenarios(scenarioList);
      setCurrentUser(meResponse);
      setUserPat(patResponse);
      setUserScenario(selectionResponse);
      setSelectedScenarioId(selectionResponse.scenarioId);
      setSelectedParamsJson(formatParamsJson(selectionResponse.paramsJson));
      if (shouldAutoShowAccessTokenModal()) {
        setAccessTokenModalOpen(true);
        markAccessTokenModalAutoShown();
      }
      await loadObservations(OBSERVATIONS_DEFAULT_PAGE, appliedFiltersRef.current);
      await loadRuntimeState();
    } catch (e) {
      if (e instanceof Error && e.message !== "Unauthorized") {
        setBannerError(e.message);
      }
    } finally {
      setIsLoading(false);
    }
  }, [loadObservations, loadRuntimeState]);

  const applyFilters = useCallback(() => {
    setAppliedFilters(filterDraft);
    void loadObservations(OBSERVATIONS_DEFAULT_PAGE, filterDraft);
  }, [filterDraft, loadObservations]);

  const clearFilters = useCallback(() => {
    setFilterDraft(EMPTY_OBSERVATION_FILTERS);
    setAppliedFilters(EMPTY_OBSERVATION_FILTERS);
    void loadObservations(OBSERVATIONS_DEFAULT_PAGE, EMPTY_OBSERVATION_FILTERS);
  }, [loadObservations]);

  const changeObservationPage = useCallback(
    (page: number) => {
      void loadObservations(page, appliedFiltersRef.current);
    },
    [loadObservations],
  );

  useEffect(() => {
    void loadManagementData();
  }, [loadManagementData]);

  useEffect(() => {
    if (isLoading) {
      return;
    }

    const splitElement = splitRef.current;
    if (!splitElement) {
      return;
    }

    const updateMaxWidth = () => {
      setMessagesPaneMaxWidth(computeMessagesPaneMaxWidth(splitElement.clientWidth));
    };

    updateMaxWidth();
    const observer = new ResizeObserver(updateMaxWidth);
    observer.observe(splitElement);
    return () => observer.disconnect();
  }, [isLoading]);

  const messagesPaneWidth = clampMessagesPaneWidth(
    messagesPanePreferredWidth,
    MESSAGES_PANE_MIN_WIDTH_PX,
    messagesPaneMaxWidth,
  );

  const handleMessagesPaneWidthChange = (next: number) => {
    setMessagesPanePreferredWidth(next);
  };

  const handleMessagesPaneWidthCommit = (next: number) => {
    const clamped = clampMessagesPaneWidth(next, MESSAGES_PANE_MIN_WIDTH_PX, messagesPaneMaxWidth);
    setMessagesPanePreferredWidth(clamped);
    writeMessagesPaneWidth(clamped);
  };

  useEffect(() => {
    const viewerEmail = currentUser?.email;
    if (!viewerEmail) {
      setStreamConnected(false);
      return;
    }

    const stopStream = runObservationStreamLoop({
      onConnected: () => setStreamConnected(true),
      onDisconnected: () => setStreamConnected(false),
      onObservation: (observation) => {
        if (!observationBelongsToViewer(observation, viewerEmail)) {
          return;
        }

        const filters = appliedFiltersRef.current;
        if (!observationMatchesFilters(observation, filters)) {
          return;
        }

        setObservationTotalCount((prev) => prev + 1);

        if (observationPageRef.current !== OBSERVATIONS_DEFAULT_PAGE) {
          return;
        }

        setObservations((prev) => upsertObservation(prev, observation, OBSERVATIONS_PAGE_SIZE));
        markObservationHighlight(observation.id);
      },
    });

    return () => {
      stopStream();
      setStreamConnected(false);
      highlightTimeoutsRef.current.forEach((timeoutId) => clearTimeout(timeoutId));
      highlightTimeoutsRef.current.clear();
    };
  }, [currentUser?.email, markObservationHighlight]);

  const runAction = async (action: () => Promise<void>, successMessage: string) => {
    setIsPending(true);
    setBannerError(null);
    try {
      await action();
      pushToast("success", successMessage);
    } catch (e) {
      const message = e instanceof Error ? e.message : "Something went wrong";
      pushToast("error", message);
    } finally {
      setIsPending(false);
    }
  };

  const saveScenario = () => {
    if (!isValidScenarioParamsJson(selectedParamsJson)) {
      pushToast("error", "Params JSON must be a valid object");
      return;
    }

    void runAction(async () => {
      const selection = await apiFetch<UserScenarioSelection>(MANAGEMENT_ME_SCENARIO_PATH, {
        method: "PUT",
        body: JSON.stringify({
          scenarioId: selectedScenarioId,
          paramsJson: selectedParamsJson,
        }),
      });
      setUserScenario(selection);
    }, "Scenario saved for your next MCP session");
  };

  const terminateSessions = () => {
    void runAction(async () => {
      await apiFetch(MANAGEMENT_ME_SESSIONS_TERMINATE_PATH, { method: "POST" });
      await loadRuntimeState();
    }, "Live MCP sessions terminated");
  };

  const handleScenarioChange = (scenarioId: string) => {
    const nextScenario = findScenario(scenarios, scenarioId);
    const currentScenario = findScenario(scenarios, selectedScenarioId);

    setSelectedScenarioId(scenarioId);

    if (!nextScenario) {
      return;
    }

    const shouldLoadExample =
      selectedParamsJson.trim() === "{}" ||
      (currentScenario !== undefined &&
        paramsMatchExample(selectedParamsJson, currentScenario.paramsExampleJson));

    if (shouldLoadExample) {
      setSelectedParamsJson(formatParamsJson(nextScenario.paramsExampleJson));
    }
  };

  const regeneratePat = () => {
    void runAction(async () => {
      const patResponse = await apiFetch<UserMcpPat>(MANAGEMENT_ME_PAT_REGENERATE_PATH, { method: "POST" });
      setUserPat(patResponse);
    }, "New access token issued");
  };

  const copyPat = () => {
    if (!userPat) {
      return;
    }

    void navigator.clipboard.writeText(userPat.pat).then(
      () => pushToast("success", "Token copied to clipboard"),
      () => pushToast("error", "Could not copy token"),
    );
  };

  const copyClientConfig = () => {
    if (!userPat) {
      return;
    }

    const configJson = buildMcpClientConfigJson(userPat.pat, window.location.origin);
    void navigator.clipboard.writeText(configJson).then(
      () => pushToast("success", "MCP config copied to clipboard"),
      () => pushToast("error", "Could not copy MCP config"),
    );
  };

  const submitFeedback = async (message: string) => {
    setIsPending(true);
    try {
      await apiFetch(MANAGEMENT_FEEDBACK_PATH, {
        method: "POST",
        body: JSON.stringify({ message }),
      });
      setFeedbackModalOpen(false);
      pushToast("success", FEEDBACK_SEND_SUCCESS);
    } catch (e) {
      const errorMessage = e instanceof Error ? e.message : FEEDBACK_SEND_ERROR;
      pushToast("error", errorMessage);
    } finally {
      setIsPending(false);
    }
  };

  const eraseEverything = async (confirmation: string) => {
    setIsPending(true);
    try {
      await apiFetch(MANAGEMENT_ME_ERASE_PATH, {
        method: "POST",
        body: JSON.stringify({ confirmation }),
      });
      clearAccessTokenModalAutoShown();
      logoutToLoginPage();
    } catch (e) {
      const errorMessage = e instanceof Error ? e.message : ERASE_EVERYTHING_SUBMIT_ERROR;
      pushToast("error", errorMessage);
      setIsPending(false);
    }
  };

  const clearObservations = () => {
    if (observationTotalCount === 0) {
      return;
    }

    if (!window.confirm(CLEAR_ALL_OBSERVATIONS_CONFIRM)) {
      return;
    }

    void runAction(async () => {
      await apiFetch(MANAGEMENT_OBSERVATIONS_PATH, { method: "DELETE" });
      setObservations([]);
      setObservationTotalCount(0);
      setSelectedObservation(null);
      setRecentObservationIds(new Set());
    }, CLEAR_ALL_OBSERVATIONS_SUCCESS);
  };

  const lastClientProtocolVersion = useMemo(
    () => observations.find((observation) => observation.clientProtocolVersion)?.clientProtocolVersion,
    [observations],
  );

  const lastDurationMs = observations[0]?.durationMs;

  return (
    <div className="flex h-dvh flex-col overflow-hidden bg-insp-bg text-insp-body">
      <AppHeader
        serverMcpProtocolVersion={currentUser?.mcpProtocolVersion}
        streamConnected={streamConnected}
        lastDurationMs={lastDurationMs}
        mobilePane={mobilePane}
        onMobilePaneChange={setMobilePane}
        onOpenAccessToken={() => setAccessTokenModalOpen(true)}
        onOpenFeedback={() => setFeedbackModalOpen(true)}
        onOpenEraseEverything={() => setEraseEverythingModalOpen(true)}
        canAccessAdmin={currentUser?.canAccessAdmin ?? false}
      />

      {bannerError && (
        <div className="shrink-0 border-b border-insp-error/40 bg-insp-error-bg px-4 py-2.5 text-sm text-insp-error" role="alert">
          {bannerError}
        </div>
      )}

      <div className="relative flex min-h-0 flex-1 flex-col">
        {isLoading ? (
          <div className="flex flex-1 items-center justify-center p-6">
            <p className="text-sm text-insp-muted">Loading {APP_NAME}…</p>
          </div>
        ) : (
          <main
            ref={splitRef}
            className="flex min-h-0 flex-1 gap-3 p-3 md:gap-0 md:p-4"
            style={{ [MESSAGES_PANE_WIDTH_VAR]: `${messagesPaneWidth}px` } as CSSProperties}
          >
            <aside
              className={`${WORKSPACE_PANE} ${
                mobilePane === CONSOLE_PANE_SCENARIO ? "flex flex-1" : "hidden"
              } md:flex md:min-w-0 md:flex-1`}
              style={{ minWidth: SCENARIOS_PANE_MIN_WIDTH_PX }}
            >
              <ControlPanel
                scenarios={scenarios}
                userScenario={userScenario}
                liveSessionCount={runtimeState?.liveSessions.length ?? 0}
                selectedScenarioId={selectedScenarioId}
                selectedParamsJson={selectedParamsJson}
                isPending={isPending}
                mcpStateless={currentUser?.mcpStateless ?? true}
                onSelectedScenarioIdChange={handleScenarioChange}
                onSelectedParamsJsonChange={setSelectedParamsJson}
                onSaveScenario={saveScenario}
                onTerminateSessions={terminateSessions}
              />
            </aside>

            <ResizeHandle
              value={messagesPaneWidth}
              min={MESSAGES_PANE_MIN_WIDTH_PX}
              max={messagesPaneMaxWidth}
              onChange={handleMessagesPaneWidthChange}
              onCommit={handleMessagesPaneWidthCommit}
            />

            <div
              className={`${WORKSPACE_PANE} ${
                mobilePane === CONSOLE_PANE_MESSAGES ? "flex flex-1" : "hidden"
              } md:flex md:w-[var(--messages-pane-width)] md:flex-none`}
            >
              <ObservationsPanel
                observations={observations}
                totalCount={observationTotalCount}
                page={observationPage}
                pageSize={OBSERVATIONS_PAGE_SIZE}
                filters={filterDraft}
                hasActiveFilters={hasActiveObservationFilters(appliedFilters)}
                streamConnected={streamConnected}
                recentObservationIds={recentObservationIds}
                isLoadingObservations={isLoadingObservations}
                onFiltersChange={setFilterDraft}
                onApplyFilters={applyFilters}
                onClearFilters={clearFilters}
                onPageChange={changeObservationPage}
                onObservationSelect={setSelectedObservation}
                onClearObservations={clearObservations}
                isClearingObservations={isPending}
              />
            </div>
          </main>
        )}

        <ObservationDetailDrawer
          observation={selectedObservation}
          onClose={() => setSelectedObservation(null)}
        />

        <AccessTokenModal
          open={accessTokenModalOpen}
          userPat={userPat}
          isPending={isPending}
          onClose={() => setAccessTokenModalOpen(false)}
          onCopyPat={copyPat}
          onCopyClientConfig={copyClientConfig}
          onRegeneratePat={regeneratePat}
        />

        <FeedbackModal
          open={feedbackModalOpen}
          isPending={isPending}
          onClose={() => setFeedbackModalOpen(false)}
          onSubmit={submitFeedback}
        />

        <EraseEverythingModal
          open={eraseEverythingModalOpen}
          isPending={isPending}
          onClose={() => setEraseEverythingModalOpen(false)}
          onConfirm={eraseEverything}
        />
      </div>

      <footer className="flex h-8 shrink-0 items-center justify-between gap-3 border-t border-insp-border bg-insp-gray-0 px-4 text-[11px] text-insp-muted">
        <span className="truncate">
          {displayEmail(currentUser?.email ?? userPat?.email, APP_NAME)}
        </span>
        <span className="shrink-0">
          {currentUser?.mcpProtocolVersion ? `MCP ${currentUser.mcpProtocolVersion}` : APP_NAME}
          {currentUser?.mcpStateless ? " · Stateless = true" : ""}
          {lastClientProtocolVersion ? ` · client ${lastClientProtocolVersion}` : ""}
        </span>
      </footer>

      <ToastStack toasts={toasts} onDismiss={dismissToast} />
    </div>
  );
}
