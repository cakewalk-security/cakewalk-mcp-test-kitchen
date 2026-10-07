import { Suspense, lazy, useEffect } from "react";
import {
  ADMIN_PATH,
  CONSOLE_PATH,
  LOGIN_ERROR_PATH,
  LOGIN_PATH,
  buildAccountLoginUrl,
  resolveAppRoute,
} from "./appRoutes";
import { isAuthenticatedUser } from "./authProbe";
import { deferAfterFirstPaint } from "./deferAfterFirstPaint";
import LoginErrorPage from "./LoginErrorPage";
import LoginPage from "./LoginPage";

const ConsoleApp = lazy(() => import("./ConsoleApp"));
const AdminPage = lazy(() => import("./AdminPage"));

const KNOWN_PATHS = new Set([CONSOLE_PATH, ADMIN_PATH, LOGIN_PATH, LOGIN_ERROR_PATH]);

function RouteFallback() {
  return (
    <div className="auth-page flex min-h-dvh items-center justify-center px-5" role="status" aria-live="polite">
      <p className="auth-muted m-0 text-center text-base">Loading…</p>
    </div>
  );
}

export default function AppRoot() {
  const route = resolveAppRoute(window.location.pathname);

  useEffect(() => {
    if (route !== "redirect") {
      return;
    }

    let cancelled = false;
    const cancelDeferred = deferAfterFirstPaint(() => {
      void isAuthenticatedUser().then((authenticated) => {
        if (cancelled) {
          return;
        }

        if (authenticated) {
          window.location.replace(CONSOLE_PATH);
          return;
        }

        window.location.replace(buildAccountLoginUrl());
      });
    });

    return () => {
      cancelled = true;
      cancelDeferred();
    };
  }, [route]);

  useEffect(() => {
    const pathname = window.location.pathname;
    if (!KNOWN_PATHS.has(pathname) && route !== "redirect") {
      window.location.replace(CONSOLE_PATH);
    }
  }, [route]);

  if (route === "redirect") {
    return null;
  }

  if (route === "login") {
    return <LoginPage />;
  }

  if (route === "login-error") {
    return <LoginErrorPage />;
  }

  return (
    <Suspense fallback={<RouteFallback />}>
      {route === "console" ? <ConsoleApp /> : null}
      {route === "admin" ? <AdminPage /> : null}
    </Suspense>
  );
}
