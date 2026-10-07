import { useEffect, useMemo } from "react";
import AuthLayout from "./AuthLayout";
import { APP_NAME } from "./appBranding";
import { AUTH_BTN_PRIMARY, AUTH_CARD, AUTH_EYEBROW, AUTH_LINK, AUTH_MUTED } from "./authStyles";
import { isAuthenticatedUser } from "./authProbe";
import { deferAfterFirstPaint } from "./deferAfterFirstPaint";
import {
  buildGitHubLoginUrl,
  buildGoogleLoginUrl,
  LOGIN_PATH,
  resolveReturnUrl,
} from "./appRoutes";
import { PRIVACY_POLICY_URL, TERMS_OF_SERVICE_URL } from "./marketingLinks";
import { usePageMetadata } from "./pageMetadata";

const LOGIN_METADATA = {
  title: `Sign in · ${APP_NAME}`,
  description: `Sign in to ${APP_NAME} with Google or GitHub to get your personal access token and start testing MCP clients.`,
  canonicalPath: LOGIN_PATH,
} as const;

export default function LoginPage() {
  const returnUrl = resolveReturnUrl(new URLSearchParams(window.location.search).get("returnUrl"));
  usePageMetadata(LOGIN_METADATA);

  useEffect(() => {
    let cancelled = false;
    const cancelDeferred = deferAfterFirstPaint(() => {
      void isAuthenticatedUser().then((authenticated) => {
        if (cancelled || !authenticated) {
          return;
        }

        window.location.replace(returnUrl);
      });
    });

    return () => {
      cancelled = true;
      cancelDeferred();
    };
  }, [returnUrl]);

  const googleLoginUrl = useMemo(() => buildGoogleLoginUrl(returnUrl), [returnUrl]);
  const githubLoginUrl = useMemo(() => buildGitHubLoginUrl(returnUrl), [returnUrl]);

  return (
    <AuthLayout>
      <section className={AUTH_CARD} aria-labelledby="login-title">
        <p className={AUTH_EYEBROW}>Sign in</p>
        <h1
          id="login-title"
          className="auth-heading mt-3 text-[clamp(1.75rem,4vw,2rem)] leading-[1.12] tracking-[-0.015em]"
        >
          Sign in to {APP_NAME}
        </h1>
        <p className={`${AUTH_MUTED} mt-4`}>
          Signing in mints a personal access token and ties it to the scenario you pick. The server
          needs that to tell your traffic from anyone else&apos;s. Your email is recorded with requests
          and is not used for marketing.
        </p>

        <nav className="mt-8 flex flex-col gap-3" aria-label="Sign-in providers">
          <a className={AUTH_BTN_PRIMARY} href={googleLoginUrl}>
            Sign in with Google
          </a>
          <a className={AUTH_BTN_PRIMARY} href={githubLoginUrl}>
            Sign in with GitHub
          </a>
        </nav>

        <p className={`${AUTH_MUTED} mt-6 text-sm`}>
          By continuing you agree to the{" "}
          <a href={TERMS_OF_SERVICE_URL} rel="noopener noreferrer" className={AUTH_LINK}>
            Terms of Service
          </a>{" "}
          and{" "}
          <a href={PRIVACY_POLICY_URL} rel="noopener noreferrer" className={AUTH_LINK}>
            Privacy Policy
          </a>
          .
        </p>
      </section>
    </AuthLayout>
  );
}
