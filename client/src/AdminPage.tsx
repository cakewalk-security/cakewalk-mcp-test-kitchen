import { loadConsoleFonts } from "./consoleFonts";
import { useCallback, useEffect, useState } from "react";
import { MANAGEMENT_ADMIN_USAGE_PATH, MANAGEMENT_ME_PATH } from "./apiPaths";
import { APP_DESCRIPTION, APP_NAME } from "./appBranding";
import { ADMIN_PATH, CONSOLE_PATH, buildAccountLoginUrl } from "./appRoutes";
import { logoutToLoginPage } from "./logout";
import { clearAccessTokenModalAutoShown } from "./accessTokenModalSession";
import { BTN_GHOST, BTN_ICON, BTN_SECONDARY } from "./buttonStyles";
import ThemeToggle from "./ThemeToggle";
import { CARD, SECTION_HINT, SECTION_TITLE } from "./uiStyles";
import { usePageMetadata } from "./pageMetadata";
import type { AdminUsageResponse, CurrentUser } from "./types";

function formatTimestamp(value?: string): string {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString();
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className={`${CARD} p-4`}>
      <p className="text-sm font-medium text-insp-muted">{label}</p>
      <p className="mt-2 text-3xl font-semibold text-insp-heading">{value}</p>
    </div>
  );
}

function redirectToLogin(): void {
  window.location.replace(buildAccountLoginUrl());
}

export default function AdminPage() {
  usePageMetadata({
    title: `Admin · ${APP_NAME}`,
    description: APP_DESCRIPTION,
    canonicalPath: ADMIN_PATH,
  });

  useEffect(() => {
    loadConsoleFonts();
  }, []);

  const [usage, setUsage] = useState<AdminUsageResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const loadData = useCallback(async () => {
    setIsLoading(true);

    try {
      const meResponse = await fetch(MANAGEMENT_ME_PATH, {
        headers: {
          Accept: "application/json",
        },
      });

      if (!meResponse.ok) {
        redirectToLogin();
        return;
      }

      const me = (await meResponse.json()) as CurrentUser;
      if (!me.canAccessAdmin) {
        redirectToLogin();
        return;
      }

      const usageResponse = await fetch(MANAGEMENT_ADMIN_USAGE_PATH, {
        headers: {
          Accept: "application/json",
        },
      });

      if (usageResponse.status === 404 || !usageResponse.ok) {
        redirectToLogin();
        return;
      }

      const payload = (await usageResponse.json()) as AdminUsageResponse;
      setUsage(payload);
    } catch {
      redirectToLogin();
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  if (isLoading || !usage) {
    return null;
  }

  return (
    <div className="flex min-h-screen flex-col bg-insp-bg text-insp-body">
      <header className="flex h-console-header shrink-0 items-center border-b border-insp-border bg-insp-surface px-4">
        <div className="flex w-full items-center gap-3">
          <div className="min-w-0 flex-1">
            <p className="truncate text-lg font-semibold text-insp-heading">{APP_NAME}</p>
            <p className="truncate text-sm text-insp-muted">Usage overview</p>
          </div>

          <div className="flex shrink-0 items-center gap-1">
            <a href={CONSOLE_PATH} className={BTN_GHOST}>
              Console
            </a>
            <ThemeToggle />
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
          </div>
        </div>
      </header>

      <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-6 p-4 md:p-6">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-2xl font-semibold">Admin usage</h1>
            <p className={SECTION_HINT}>
              Registered users, live connections, and recent MCP activity. Emails are masked to domain only.
            </p>
          </div>
          <button type="button" className={BTN_SECONDARY} onClick={() => void loadData()} disabled={isLoading}>
            Refresh
          </button>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Registered users" value={usage.totals.registeredUserCount} />
          <StatCard label="Connected users" value={usage.totals.connectedUserCount} />
          <StatCard label="Observations (24h)" value={usage.totals.observationsLast24Hours} />
          <StatCard label="Active users (24h)" value={usage.totals.activeUsersLast24Hours} />
        </div>

        <div className={`${CARD} overflow-hidden`}>
          <div className="border-b border-insp-border px-4 py-3">
            <h2 className={SECTION_TITLE}>Users</h2>
            <p className={SECTION_HINT}>
              {usage.users.length} user{usage.users.length === 1 ? "" : "s"} ·{" "}
              {usage.totals.liveSessionCount} live session{usage.totals.liveSessionCount === 1 ? "" : "s"} ·{" "}
              {usage.totals.observationsLast7Days} observations (7d)
            </p>
          </div>

          {usage.users.length === 0 ? (
            <p className="px-4 py-8 text-center text-sm text-insp-muted">No registered or active users yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-left text-sm">
                <thead className="bg-insp-gray-0 text-xs font-semibold tracking-wide text-insp-muted uppercase">
                  <tr>
                    <th className="px-4 py-3">User</th>
                    <th className="px-4 py-3">Registered</th>
                    <th className="px-4 py-3">Last seen</th>
                    <th className="px-4 py-3">24h</th>
                    <th className="px-4 py-3">7d</th>
                    <th className="px-4 py-3">Live</th>
                    <th className="px-4 py-3">Last scenario</th>
                  </tr>
                </thead>
                <tbody>
                  {usage.users.map((user) => (
                    <tr key={user.maskedEmail} className="border-t border-insp-border">
                      <td className="px-4 py-3 font-medium">{user.maskedEmail}</td>
                      <td className="px-4 py-3 text-insp-muted">{formatTimestamp(user.registeredAt)}</td>
                      <td className="px-4 py-3 text-insp-muted">{formatTimestamp(user.lastSeenAt)}</td>
                      <td className="px-4 py-3">{user.observationCountLast24Hours}</td>
                      <td className="px-4 py-3">{user.observationCountLast7Days}</td>
                      <td className="px-4 py-3">{user.liveSessionCount}</td>
                      <td className="px-4 py-3 text-insp-muted">{user.lastScenarioId ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
