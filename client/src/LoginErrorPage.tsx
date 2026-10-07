import { useMemo } from "react";
import AuthLayout from "./AuthLayout";
import { AUTH_BTN_PRIMARY, AUTH_CARD, AUTH_EYEBROW, AUTH_LINK, AUTH_MUTED } from "./authStyles";
import {
  buildGitHubLoginUrl,
  buildGoogleLoginUrl,
  LOGIN_ERROR_PATH,
  LOGIN_PATH,
  resolveReturnUrl,
} from "./appRoutes";
import { APP_NAME } from "./appBranding";
import { resolveLoginErrorCopy } from "./loginErrorCopy";
import { usePageMetadata } from "./pageMetadata";

export default function LoginErrorPage() {
  const params = useMemo(() => new URLSearchParams(window.location.search), []);
  const code = params.get("code");
  const copy = resolveLoginErrorCopy(code);
  const returnUrl = resolveReturnUrl(params.get("returnUrl"));

  usePageMetadata({
    title: `${copy.title} · ${APP_NAME}`,
    description: copy.detail,
    canonicalPath: LOGIN_ERROR_PATH,
  });

  const googleLoginUrl = useMemo(() => buildGoogleLoginUrl(returnUrl), [returnUrl]);
  const githubLoginUrl = useMemo(() => buildGitHubLoginUrl(returnUrl), [returnUrl]);

  return (
    <AuthLayout skipLinkLabel="Skip to sign-in options">
      <section className={AUTH_CARD} aria-labelledby="login-error-title">
        <p className={AUTH_EYEBROW}>Sign-in error</p>
        <h1
          id="login-error-title"
          className="auth-heading mt-3 text-[clamp(1.75rem,4vw,2rem)] leading-[1.12] tracking-[-0.015em]"
        >
          {copy.title}
        </h1>
        <p className={`${AUTH_MUTED} mt-4`}>{copy.detail}</p>

        <nav className="mt-8 flex flex-col gap-3" aria-label="Sign-in providers">
          <a className={AUTH_BTN_PRIMARY} href={googleLoginUrl}>
            Sign in with Google
          </a>
          <a className={AUTH_BTN_PRIMARY} href={githubLoginUrl}>
            Sign in with GitHub
          </a>
        </nav>

        <p className={`${AUTH_MUTED} mt-6 text-sm`}>
          <a className={AUTH_LINK} href={LOGIN_PATH}>
            Back to sign in
          </a>
        </p>
      </section>
    </AuthLayout>
  );
}
