import { APP_NAME, APP_TAGLINE } from "./appBranding";
import { ADMIN_PATH } from "./appRoutes";
import { clearAccessTokenModalAutoShown } from "./accessTokenModalSession";
import { logoutToLoginPage } from "./logout";
import { BTN_GHOST, BTN_ICON } from "./buttonStyles";
import {
  CONSOLE_PANE_LABEL_MESSAGES,
  CONSOLE_PANE_LABEL_SCENARIO,
  CONSOLE_PANE_MESSAGES,
  CONSOLE_PANE_SCENARIO,
  type ConsolePane,
} from "./consolePanes";
import { ERASE_EVERYTHING_TOOLTIP } from "./eraseEverything";
import LockIcon from "./LockIcon";
import ThemeToggle from "./ThemeToggle";
import Tooltip from "./Tooltip";

const CONNECTED_LABEL = "Connected";
const RECONNECTING_LABEL = "Reconnecting";

type AppHeaderProps = {
  serverMcpProtocolVersion?: string;
  streamConnected: boolean;
  lastDurationMs?: number;
  mobilePane: ConsolePane;
  onMobilePaneChange: (pane: ConsolePane) => void;
  onOpenAccessToken?: () => void;
  onOpenFeedback?: () => void;
  onOpenEraseEverything?: () => void;
  canAccessAdmin?: boolean;
};

function InspectorMark() {
  return (
    <span
      className="flex size-8 shrink-0 items-center justify-center rounded-md bg-insp-primary text-insp-ink"
      aria-hidden
    >
      <svg viewBox="0 0 20 20" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.8">
        <circle cx="6" cy="10" r="2.2" />
        <circle cx="14" cy="6.5" r="2.2" />
        <circle cx="14" cy="13.5" r="2.2" />
        <path d="M8 10h4M12.2 7.6 8.2 9.3M12.2 12.4 8.2 10.7" />
      </svg>
    </span>
  );
}

function SegmentedControl({
  value,
  onChange,
}: {
  value: ConsolePane;
  onChange: (pane: ConsolePane) => void;
}) {
  const options = [
    { id: CONSOLE_PANE_SCENARIO, label: CONSOLE_PANE_LABEL_SCENARIO },
    { id: CONSOLE_PANE_MESSAGES, label: CONSOLE_PANE_LABEL_MESSAGES },
  ] as const;

  return (
    <div className="inline-flex rounded-md bg-insp-gray-0 p-1" role="tablist" aria-label="Console panes">
      {options.map((option) => {
        const selected = value === option.id;
        return (
          <button
            key={option.id}
            type="button"
            role="tab"
            aria-selected={selected}
            className={`rounded-[5px] px-3 py-1 text-sm font-medium transition-colors ${
              selected
                ? "bg-insp-surface text-insp-heading shadow-sm"
                : "text-insp-muted hover:text-insp-heading"
            }`}
            onClick={() => onChange(option.id)}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}

export default function AppHeader({
  serverMcpProtocolVersion,
  streamConnected,
  lastDurationMs,
  mobilePane,
  onMobilePaneChange,
  onOpenAccessToken,
  onOpenFeedback,
  onOpenEraseEverything,
  canAccessAdmin = false,
}: AppHeaderProps) {
  const statusLabel = streamConnected
    ? lastDurationMs === undefined
      ? CONNECTED_LABEL
      : `${CONNECTED_LABEL} (${lastDurationMs}ms)`
    : RECONNECTING_LABEL;

  return (
    <header className="relative z-20 flex h-console-header shrink-0 items-center border-b border-insp-border bg-insp-surface">
      <div className="flex w-full items-center gap-3 px-3 md:px-4">
        <div className="flex min-w-0 flex-1 items-center gap-2.5">
          <InspectorMark />
          <p
            className="min-w-0 flex-1 truncate text-lg font-semibold text-insp-heading"
            title={APP_TAGLINE}
          >
            {APP_NAME}
          </p>
          {serverMcpProtocolVersion ? (
            <span className="hidden shrink-0 rounded bg-insp-gray-0 px-1.5 py-0.5 text-[10px] font-semibold tracking-wide text-insp-muted uppercase sm:inline">
              {serverMcpProtocolVersion}
            </span>
          ) : null}
        </div>

        <div className="md:hidden">
          <SegmentedControl value={mobilePane} onChange={onMobilePaneChange} />
        </div>

        <div className="flex shrink-0 items-center gap-0.5">
          {canAccessAdmin ? (
            <a href={ADMIN_PATH} className={`${BTN_GHOST} hidden sm:inline-flex`}>
              Admin
            </a>
          ) : null}
          <span
            className={`mr-1 hidden items-center gap-1.5 px-2 text-sm font-medium sm:inline-flex ${
              streamConnected ? "text-insp-success-fg" : "text-insp-warning-fg"
            }`}
          >
            <span
              className={`size-2 rounded-full ${
                streamConnected ? "bg-insp-success" : "bg-insp-warning"
              }`}
              aria-hidden
            />
            {statusLabel}
          </span>

          {onOpenAccessToken && (
            <Tooltip label="MCP access token" placement="bottom">
              <button
                type="button"
                className={BTN_ICON}
                onClick={onOpenAccessToken}
                aria-label="Open MCP access token"
              >
                <LockIcon />
              </button>
            </Tooltip>
          )}
          {onOpenFeedback && (
            <Tooltip label="Give feedback" placement="bottom">
              <button
                type="button"
                className={BTN_ICON}
                onClick={onOpenFeedback}
                aria-label="Give feedback"
              >
                <svg className="size-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path
                    fillRule="evenodd"
                    d="M2.5 3A1.5 1.5 0 001 4.5v6A1.5 1.5 0 002.5 12H6v3.33a.75.75 0 001.11.67L12.11 12h5.39A1.5 1.5 0 0019 10.5v-6A1.5 1.5 0 0017.5 3h-15z"
                    clipRule="evenodd"
                  />
                </svg>
              </button>
            </Tooltip>
          )}
          <ThemeToggle />
          {onOpenEraseEverything && (
            <Tooltip label={ERASE_EVERYTHING_TOOLTIP} placement="bottom">
              <button
                type="button"
                className={BTN_ICON}
                onClick={onOpenEraseEverything}
                aria-label={ERASE_EVERYTHING_TOOLTIP}
              >
                <svg viewBox="0 0 24 24" className="size-4" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
                  <polyline points="3 6 5 6 21 6" />
                  <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                  <line x1="10" y1="11" x2="10" y2="17" />
                  <line x1="14" y1="11" x2="14" y2="17" />
                </svg>
              </button>
            </Tooltip>
          )}
          <Tooltip label="Sign out" placement="bottom">
            <button
              type="button"
              className={BTN_ICON}
              aria-label="Sign out"
              onClick={() => {
                clearAccessTokenModalAutoShown();
                logoutToLoginPage();
              }}
            >
              <svg viewBox="0 0 24 24" className="size-4" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                <polyline points="16 17 21 12 16 7" />
                <line x1="21" y1="12" x2="9" y2="12" />
              </svg>
            </button>
          </Tooltip>
        </div>
      </div>
    </header>
  );
}
