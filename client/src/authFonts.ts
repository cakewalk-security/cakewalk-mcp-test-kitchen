import { loadStylesheet } from "./loadStylesheet";

const AUTH_FONTS_ID = "auth-fonts";
const AUTH_FONTS_HREF =
  "https://fonts.googleapis.com/css2?family=Bitter:wght@400&family=Rethink+Sans:wght@400;600;700&display=swap";

let preconnectAdded = false;

function ensureFontPreconnect(): void {
  if (preconnectAdded) {
    return;
  }

  for (const { href, crossOrigin } of [
    { href: "https://fonts.googleapis.com", crossOrigin: false },
    { href: "https://fonts.gstatic.com", crossOrigin: true },
  ]) {
    if (document.querySelector(`link[rel="preconnect"][href="${href}"]`)) {
      continue;
    }

    const link = document.createElement("link");
    link.rel = "preconnect";
    link.href = href;
    if (crossOrigin) {
      link.crossOrigin = "";
    }
    document.head.appendChild(link);
  }

  preconnectAdded = true;
}

export function loadAuthFonts(): void {
  ensureFontPreconnect();
  loadStylesheet(AUTH_FONTS_HREF, AUTH_FONTS_ID);
}
