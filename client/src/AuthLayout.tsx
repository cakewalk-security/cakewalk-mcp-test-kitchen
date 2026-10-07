import { useEffect, type ReactNode } from "react";
import { loadAuthFonts } from "./authFonts";
import { APP_NAME_MARK } from "./appBranding";
import {
  CAKEWALK_HOME_URL,
  IMPRINT_URL,
  MARKETING_HOME_URL,
  PRIVACY_POLICY_URL,
  TERMS_OF_SERVICE_URL,
} from "./marketingLinks";

const EXTERNAL_LINK_REL = "noopener noreferrer";

type AuthLayoutProps = {
  children: ReactNode;
  skipLinkLabel?: string;
};

export default function AuthLayout({ children, skipLinkLabel = "Skip to sign in" }: AuthLayoutProps) {
  useEffect(() => {
    loadAuthFonts();
  }, []);

  return (
    <div className="auth-page flex min-h-dvh flex-col">
      <a href="#main-content" className="auth-skip-link">
        {skipLinkLabel}
      </a>

      <header className="auth-header sticky top-0 z-30">
        <div className="auth-header-inner mx-auto flex w-full max-w-[1300px] items-center">
          <div className="auth-brand">
            <a href={MARKETING_HOME_URL} rel={EXTERNAL_LINK_REL} className="auth-brand-name">
              {APP_NAME_MARK}
            </a>
            <span className="auth-brand-by">
              <span>powered by</span>
              <a href={CAKEWALK_HOME_URL} rel={EXTERNAL_LINK_REL} aria-label="Cakewalk home">
                <img
                  src="/cakewalk-logo-white.svg"
                  alt="Cakewalk"
                  width={72}
                  height={18}
                  decoding="async"
                />
              </a>
            </span>
          </div>
        </div>
      </header>

      <main
        id="main-content"
        tabIndex={-1}
        className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center px-5 py-10 sm:px-7"
      >
        {children}
      </main>

      <footer className="auth-footer">
        <div className="auth-footer-inner mx-auto flex w-full max-w-[1300px] flex-wrap items-center justify-between">
          <a href={CAKEWALK_HOME_URL} rel={EXTERNAL_LINK_REL} className="auth-footer-by">
            <span>Powered by</span>
            <img
              src="/cakewalk-logo-white.svg"
              alt="Cakewalk"
              width={82}
              height={21}
              loading="lazy"
              decoding="async"
            />
          </a>
          <nav className="auth-footer-links" aria-label="Legal">
            <a href={PRIVACY_POLICY_URL} rel={EXTERNAL_LINK_REL}>
              Privacy
            </a>
            <a href={TERMS_OF_SERVICE_URL} rel={EXTERNAL_LINK_REL}>
              Terms
            </a>
            <a href={IMPRINT_URL} target="_blank" rel={EXTERNAL_LINK_REL}>
              Imprint
            </a>
          </nav>
        </div>
      </footer>
    </div>
  );
}
